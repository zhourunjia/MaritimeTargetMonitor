from edgeyolo.models import EdgeYOLO

# ---------------- 参数 ----------------
CFG_PATH    = "cfg/edgeyolo_tiny.yaml"      # 你的配置文件路径
WEIGHT_PATH = "best.pth"                    # 训练权重路径
VIDEO_PATH  = "input.mp4"                    # 0 表示摄像头
CONF_THRES  = 0.4
IOU_THRES   = 0.45

# ---------------- 载入模型 ----------------
# EdgeYOLO 内部会自动放到 CUDA（若可用）并设为 eval 模式
model = EdgeYOLO(cfg_file=CFG_PATH, weights=WEIGHT_PATH)

# ---------------- 直接预测 ----------------
# save=True  会自动在 runs/ 目录下保存带框视频
# show=True  会直接弹窗显示检测结果
results = model.predict(
    source=VIDEO_PATH,      # 也可以写 0 调用摄像头
    conf=CONF_THRES,
    iou=IOU_THRES,
    save=True,
    show=True
)

print("检测完成。结果保存在 runs/detect 下。")
