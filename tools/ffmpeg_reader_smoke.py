import os
import subprocess
import time


FFMPEG = r"C:\Users\15666\Desktop\MaritimeTargetMonitor\MaritimeTargetMonitor\Maritime.App\bin\x64\Release\net48\ffmpeg-full\bin\ffmpeg.exe"
SOURCE = "rtmp://127.0.0.1:1935/live/raw"
WIDTH = 1280
HEIGHT = 720


def build_cmd():
    cmd = [
        FFMPEG,
        "-hide_banner",
        "-loglevel",
        "error",
        "-fflags",
        "nobuffer",
        "-flags",
        "low_delay",
        "-rtmp_live",
        "live",
        "-i",
        SOURCE,
        "-an",
        "-sn",
        "-vf",
        f"scale={WIDTH}:{HEIGHT}",
        "-pix_fmt",
        "bgr24",
        "-f",
        "rawvideo",
        "-",
    ]
    return cmd


def main():
    frame_size = WIDTH * HEIGHT * 3
    cmd = build_cmd()
    print("cmd:", " ".join(cmd))
    proc = subprocess.Popen(
        cmd,
        stdin=subprocess.DEVNULL,
        stdout=subprocess.PIPE,
        stderr=subprocess.DEVNULL,
        bufsize=frame_size * 4,
    )
    t0 = time.time()
    data = proc.stdout.read(frame_size)
    dt = time.time() - t0
    print("read bytes:", 0 if data is None else len(data), "time:", f"{dt:.2f}s")
    proc.terminate()


if __name__ == "__main__":
    main()
