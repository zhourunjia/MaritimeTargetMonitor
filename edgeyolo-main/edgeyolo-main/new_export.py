import torch
from edgeyolo import EdgeYOLO

edge = EdgeYOLO("params/model/my_edgeyolo_tiny.yaml")
ckpt = torch.load("best.pth", map_location="cpu")
edge.try_load_state_dict(ckpt['model'] if 'model' in ckpt else ckpt)
model = edge.model.eval()

dummy = torch.randn(1, 3, 640, 640)  # 按训练时输入尺寸
torch.onnx.export(
    model, dummy, "my_edgeyolo_tiny.onnx",
    input_names=["images"], output_names=["output"],
    opset_version=12, dynamic_axes={"images":{0:"batch"}}
)
