import argparse
import os
import sys
import time
import subprocess
import json
import re
from copy import deepcopy

import cv2
import numpy as np
import torch

# Local EdgeYOLO algorithm modules
from edgeyolo.detect import Detector, TRTDetector, draw

os.environ.setdefault("QT_QPA_PLATFORM", "offscreen")


def parse_args():
    parser = argparse.ArgumentParser("EdgeYOLO local stream infer")
    parser.add_argument("-w", "--weights", type=str, required=True, help="weight file")
    parser.add_argument("-s", "--source", type=str, required=True, help="video source, file/rtsp/rtmp/camera id")
    parser.add_argument("-o", "--output", type=str, required=True, help="output url or file path")

    parser.add_argument("-c", "--conf-thres", type=float, default=0.25, help="confidence threshold")
    parser.add_argument("-n", "--nms-thres", type=float, default=0.55, help="nms threshold")
    parser.add_argument("--input-size", type=int, nargs="+", default=[640, 640], help="input size: [height, width]")
    parser.add_argument("--fp16", action="store_true", help="fp16")
    parser.add_argument("--no-fuse", action="store_true", help="do not fuse model")
    parser.add_argument("--trt", action="store_true", help="is trt model")
    parser.add_argument("--cpu", action="store_true", help="force CPU inference")
    parser.add_argument("--legacy", action="store_true", help="if img /= 255 while training, add this command.")
    parser.add_argument("--use-decoder", action="store_true", help="support original yolox model v0.2.0")
    parser.add_argument("--batch", type=int, default=1, help="batch size")
    parser.add_argument("--no-label", action="store_true", help="do not draw label")

    parser.add_argument("--fps", type=float, default=0, help="output fps, 0 = auto")
    parser.add_argument("--cap-fps", type=float, default=0, help="limit processing fps, 0 = no limit")
    parser.add_argument("--out-size", type=str, nargs="+", default=None, help="output size: width height or WxH")
    parser.add_argument("--ffmpeg", type=str, default="ffmpeg", help="ffmpeg executable path")
    parser.add_argument("--no-infer", action="store_true", help="bypass inference for pipeline testing")
    parser.add_argument("--max-frames", type=int, default=0, help="stop after N frames, 0 = no limit")
    parser.add_argument("--retry", type=int, default=30, help="retry count when opening stream, 0 = no retry, -1 = forever")
    parser.add_argument("--retry-interval", type=float, default=1.0, help="seconds between retries")
    return parser.parse_args()


def _open_capture(source: str):
    if source.isdigit():
        cap = cv2.VideoCapture(int(source))
    else:
        cap = cv2.VideoCapture(source)
    return cap


def _is_stream_source(source: str):
    if source.isdigit():
        return True
    return "://" in source


def _guess_ffprobe_path(ffmpeg_path: str):
    if not ffmpeg_path:
        return "ffprobe"
    name = os.path.basename(ffmpeg_path).lower()
    if "ffmpeg" in name:
        base = os.path.dirname(ffmpeg_path)
        if os.name == "nt":
            cand = os.path.join(base, "ffprobe.exe")
        else:
            cand = os.path.join(base, "ffprobe")
        if os.path.exists(cand):
            return cand
    return "ffprobe"


def _parse_rate(rate_text: str):
    if not rate_text:
        return None
    if "/" in rate_text:
        try:
            num, den = rate_text.split("/", 1)
            num = float(num)
            den = float(den)
            if den != 0:
                return num / den
        except Exception:
            return None
    try:
        return float(rate_text)
    except Exception:
        return None


def _probe_stream_meta(ffmpeg_path: str, source: str):
    width = height = None
    fps = None
    ffprobe = _guess_ffprobe_path(ffmpeg_path)
    try:
        cmd = [
            ffprobe,
            "-v",
            "error",
            "-select_streams",
            "v:0",
            "-show_entries",
            "stream=width,height,r_frame_rate",
            "-of",
            "json",
            source,
        ]
        res = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, timeout=5)
        if res.returncode == 0 and res.stdout:
            data = json.loads(res.stdout)
            if data.get("streams"):
                stream = data["streams"][0]
                width = stream.get("width")
                height = stream.get("height")
                fps = _parse_rate(stream.get("r_frame_rate"))
                return width, height, fps
    except FileNotFoundError:
        pass
    except Exception:
        pass

    try:
        cmd = [
            ffmpeg_path,
            "-hide_banner",
            "-loglevel",
            "error",
            "-i",
            source,
            "-t",
            "0.1",
            "-f",
            "null",
            "-",
        ]
        res = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, timeout=5)
        text = res.stderr or ""
        m = re.search(r"(?P<w>\\d{2,5})x(?P<h>\\d{2,5})", text)
        if m:
            width = int(m.group("w"))
            height = int(m.group("h"))
        m2 = re.search(r"(?P<fps>\\d+(?:\\.\\d+)?)\\s*fps", text)
        if m2:
            fps = float(m2.group("fps"))
    except Exception:
        pass

    return width, height, fps


def _build_ffmpeg_input_cmd(ffmpeg_path: str, source: str, width: int, height: int):
    cmd = [
        ffmpeg_path,
        "-hide_banner",
        "-loglevel",
        "error",
        "-fflags",
        "nobuffer",
        "-flags",
        "low_delay",
    ]
    lower = source.lower()
    if lower.startswith("rtmp://") or lower.startswith("rtmps://"):
        cmd += ["-rtmp_live", "live"]
    if lower.startswith("rtsp://") or lower.startswith("rtsps://"):
        cmd += ["-rtsp_transport", "tcp"]

    cmd += [
        "-i",
        source,
        "-an",
        "-sn",
        "-vf",
        f"scale={width}:{height}",
        "-pix_fmt",
        "bgr24",
        "-f",
        "rawvideo",
        "-",
    ]
    return cmd


class FFmpegReader:
    def __init__(self, cmd, width, height):
        self.width = width
        self.height = height
        self.frame_size = width * height * 3
        self.proc = subprocess.Popen(
            cmd,
            stdin=subprocess.DEVNULL,
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            bufsize=self.frame_size * 4,
        )

    def read(self):
        if self.proc.poll() is not None:
            return False, None
        data = self.proc.stdout.read(self.frame_size)
        if not data or len(data) < self.frame_size:
            return False, None
        frame = np.frombuffer(data, dtype=np.uint8).reshape((self.height, self.width, 3))
        return True, frame

    def close(self):
        try:
            if self.proc.stdout:
                self.proc.stdout.close()
        except Exception:
            pass
        try:
            if self.proc.stderr:
                self.proc.stderr.close()
        except Exception:
            pass
        try:
            self.proc.terminate()
        except Exception:
            pass


def _open_capture_with_retry(source: str, retries: int, interval: float, allow_retry: bool):
    attempt = 0
    while True:
        cap = _open_capture(source)
        if cap.isOpened():
            ok, frame = cap.read()
            if ok and frame is not None:
                return cap, frame
        try:
            cap.release()
        except Exception:
            pass

        if not allow_retry or retries == 0:
            return None, None

        attempt += 1
        if retries > 0 and attempt > retries:
            return None, None

        if retries < 0:
            msg = f"[warn] failed to open source: {source}, retry {attempt}/inf"
        else:
            msg = f"[warn] failed to open source: {source}, retry {attempt}/{retries}"
        print(msg)
        time.sleep(max(0.1, float(interval)))


def _open_ffmpeg_reader_with_retry(source: str, ffmpeg_path: str, out_size, input_size, retries: int, interval: float):
    attempt = 0
    width, height, fps = _probe_stream_meta(ffmpeg_path, source)
    # Prefer decoding to input_size to reduce CPU load; output_size is handled after inference.
    if input_size is not None:
        width, height = input_size
    elif width is None or height is None:
        if out_size is not None:
            width, height = out_size
        else:
            width, height = 1280, 720

    while True:
        cmd = _build_ffmpeg_input_cmd(ffmpeg_path, source, width, height)
        try:
            reader = FFmpegReader(cmd, width, height)
            ok, frame = reader.read()
            if ok and frame is not None:
                return reader, frame, fps, (width, height)
            reader.close()
        except FileNotFoundError:
            print(f"[error] ffmpeg not found: {ffmpeg_path}")
            return None, None, None, None
        except Exception:
            pass

        attempt += 1
        if retries == 0 or (retries > 0 and attempt > retries):
            return None, None, None, None

        if retries < 0:
            msg = f"[warn] failed to open source via ffmpeg: {source}, retry {attempt}/inf"
        else:
            msg = f"[warn] failed to open source via ffmpeg: {source}, retry {attempt}/{retries}"
        print(msg)
        time.sleep(max(0.1, float(interval)))


def _get_fps(cap, fallback=25.0):
    fps = cap.get(cv2.CAP_PROP_FPS)
    if fps is None or fps <= 1e-3 or fps != fps:
        return fallback
    return fps


def _is_stream_url(output: str):
    return "://" in output


def _build_ffmpeg_cmd(ffmpeg, width, height, fps, output):
    pix_fmt = "bgr24"
    size = f"{width}x{height}"
    base = [
        ffmpeg,
        "-hide_banner",
        "-loglevel",
        "error",
        "-re",
        "-f",
        "rawvideo",
        "-pix_fmt",
        pix_fmt,
        "-s",
        size,
        "-r",
        str(fps),
        "-i",
        "-",
        "-an",
        "-c:v",
        "libx264",
        "-preset",
        "veryfast",
        "-tune",
        "zerolatency",
        "-pix_fmt",
        "yuv420p",
    ]

    lower = output.lower()
    if lower.startswith("rtsp://") or lower.startswith("rtsps://"):
        base += ["-f", "rtsp", "-rtsp_transport", "tcp", output]
    elif lower.startswith("rtmp://") or lower.startswith("rtmps://"):
        base += ["-f", "flv", output]
    else:
        base += [output]
    return base


def _parse_out_size(size_list):
    if not size_list:
        return None
    parts = []
    if len(size_list) == 1:
        raw = str(size_list[0])
        for token in raw.replace("x", " ").replace("X", " ").replace(",", " ").split():
            parts.append(token)
    else:
        parts = list(size_list[:2])
    if len(parts) < 2:
        return None
    try:
        w = int(float(parts[0]))
        h = int(float(parts[1]))
    except Exception:
        return None
    if w <= 0 or h <= 0:
        return None
    return w, h


def main():
    args = parse_args()

    if not os.path.exists(args.weights):
        print(f"[error] weights not found: {args.weights}")
        return 2

    # Fallback to CPU when no CUDA device is available
    if not args.cpu and not args.trt:
        try:
            if not torch.cuda.is_available():
                print("[warn] CUDA not available, switching to CPU inference.")
                args.cpu = True
        except Exception:
            # If CUDA check fails, default to CPU to avoid crash
            print("[warn] CUDA check failed, switching to CPU inference.")
            args.cpu = True

    is_stream = _is_stream_source(args.source)
    retry_count = args.retry if is_stream else 0

    cap = None
    reader = None
    first = None

    in_size = _parse_out_size(args.input_size)
    out_size = _parse_out_size(args.out_size)

    if is_stream:
        reader, first, probed_fps, _ = _open_ffmpeg_reader_with_retry(
            args.source, args.ffmpeg, out_size, in_size, retry_count, args.retry_interval
        )
        if reader is None or first is None:
            print(f"[error] failed to open source after retries: {args.source}")
            return 2
        source_fps = probed_fps if probed_fps and probed_fps > 0 else 25.0
    else:
        cap, first = _open_capture_with_retry(args.source, retry_count, args.retry_interval, True)
        if cap is None or first is None:
            print(f"[error] failed to open source after retries: {args.source}")
            return 2
        source_fps = _get_fps(cap)
    fps = args.fps if args.fps > 0 else source_fps
    if args.cap_fps > 0 and (fps <= 0 or args.cap_fps < fps):
        fps = args.cap_fps

    height, width = first.shape[:2]
    if out_size is not None:
        out_width, out_height = out_size
    else:
        out_width, out_height = width, height

    # Local EdgeYOLO detector
    detect = None
    if not args.no_infer:
        detector_cls = TRTDetector if args.trt else Detector
        detect = detector_cls(
            weight_file=args.weights,
            conf_thres=args.conf_thres,
            nms_thres=args.nms_thres,
            input_size=args.input_size,
            fuse=not args.no_fuse,
            fp16=args.fp16,
            cpu=args.cpu,
            use_decoder=args.use_decoder,
        )

    ffmpeg_proc = None
    if _is_stream_url(args.output):
        cmd = _build_ffmpeg_cmd(args.ffmpeg, out_width, out_height, fps, args.output)
        try:
            ffmpeg_proc = subprocess.Popen(
                cmd,
                stdin=subprocess.PIPE,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.PIPE,
            )
        except FileNotFoundError:
            print(f"[error] ffmpeg not found: {args.ffmpeg}")
            return 3
    else:
        print("[error] output must be a stream url (rtmp/rtsp).")
        return 3

    try:
        frames = [first]
        last_emit = time.time() if args.cap_fps > 0 else 0.0
        min_interval = 1.0 / args.cap_fps if args.cap_fps > 0 else 0.0
        frame_count = 0
        t0 = time.time()

        while True:
            while len(frames) < args.batch:
                if reader is not None:
                    ok, frame = reader.read()
                    if not ok:
                        if not is_stream:
                            break
                        try:
                            reader.close()
                        except Exception:
                            pass
                        reader, frame, probed_fps, _ = _open_ffmpeg_reader_with_retry(
                            args.source, args.ffmpeg, out_size, in_size, retry_count, args.retry_interval
                        )
                        if reader is None or frame is None:
                            break
                else:
                    ok, frame = cap.read()
                    if not ok:
                        if not is_stream:
                            break
                        try:
                            cap.release()
                        except Exception:
                            pass
                        cap, frame = _open_capture_with_retry(args.source, retry_count, args.retry_interval, is_stream)
                        if cap is None or frame is None:
                            break
                if args.cap_fps > 0:
                    now = time.time()
                    if last_emit > 0 and now - last_emit < min_interval:
                        continue
                    last_emit = now
                frames.append(frame)

            if len(frames) == 0:
                break

            if detect is None:
                imgs = frames
            else:
                results = detect(frames, args.legacy)
                imgs = draw(deepcopy(frames), results, detect.class_names, 2, draw_label=not args.no_label)

            for img in imgs:
                if img.shape[1] != out_width or img.shape[0] != out_height:
                    img = cv2.resize(img, (out_width, out_height))
                if not img.flags["C_CONTIGUOUS"]:
                    img = np.ascontiguousarray(img)

                if ffmpeg_proc.poll() is not None:
                    err = ffmpeg_proc.stderr.read().decode("utf-8", errors="ignore")
                    print(f"[error] ffmpeg exited: {err}")
                    return 4
                try:
                    ffmpeg_proc.stdin.write(img.tobytes())
                except (BrokenPipeError, OSError):
                    err = ffmpeg_proc.stderr.read().decode("utf-8", errors="ignore")
                    print(f"[error] ffmpeg pipe broken: {err}")
                    return 4
                frame_count += 1
                if args.max_frames > 0 and frame_count >= args.max_frames:
                    break

            frames = []

            if frame_count % 100 == 0:
                dt = time.time() - t0
                if dt > 0:
                    print(f"[info] processed {frame_count} frames, {frame_count / dt:.1f} FPS")

            if args.max_frames > 0 and frame_count >= args.max_frames:
                break

    except KeyboardInterrupt:
        print("[info] interrupted")
    finally:
        if cap is not None:
            cap.release()
        if reader is not None:
            reader.close()
        if ffmpeg_proc is not None:
            try:
                ffmpeg_proc.stdin.close()
                ffmpeg_proc.terminate()
            except Exception:
                pass
        torch.cuda.empty_cache()

    return 0


if __name__ == "__main__":
    sys.exit(main())
