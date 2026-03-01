import torch
import tensorrt as trt
import pycuda.driver as cuda
import pycuda.autoinit
import numpy as np
from edgeyolo.models import EdgeYOLO


# 1. 加载 PyTorch 模型
def load_pytorch_model(weights_path):
    print("Loading PyTorch model...")
    model = EdgeYOLO(weights=weights_path).model
    model.eval()
    model.fuse()
    return model


# 2. 创建 TensorRT 构建器
def build_trt_engine(pytorch_model, input_shape, batch_size=1, fp16=True, int8=False, calib_dataset=None):
    logger = trt.Logger(trt.Logger.WARNING)
    builder = trt.Builder(logger)

    # 使用显式批处理模式
    network = builder.create_network(1 << int(trt.NetworkDefinitionCreationFlag.EXPLICIT_BATCH))
    config = builder.create_builder_config()

    # 设置工作空间大小 (8GB)
    config.max_workspace_size = 8 * 1 << 30

    # 设置精度
    if fp16:
        config.set_flag(trt.BuilderFlag.FP16)
    if int8:
        config.set_flag(trt.BuilderFlag.INT8)
        if calib_dataset is None:
            raise ValueError("INT8 mode requires calibration dataset")

    # 添加优化配置文件
    profile = builder.create_optimization_profile()
    input_name = "input_0"
    input_shape_tuple = (batch_size, 3, input_shape[0], input_shape[1])

    # 设置输入形状范围
    profile.set_shape(input_name,
                      input_shape_tuple,  # 最小形状
                      input_shape_tuple,  # 最优形状
                      input_shape_tuple)  # 最大形状
    config.add_optimization_profile(profile)

    # 创建输入
    input_tensor = network.add_input(name=input_name, dtype=trt.float32, shape=input_shape_tuple)

    # 转换 PyTorch 模型到 TensorRT 网络
    pytorch_model = pytorch_model.cuda()
    pytorch_model = pytorch_model.half() if fp16 else pytorch_model

    # 创建 ONNX 模型作为中间表示
    print("Exporting to ONNX...")
    dummy_input = torch.randn(input_shape_tuple).cuda()
    dummy_input = dummy_input.half() if fp16 else dummy_input

    with torch.no_grad():
        torch.onnx.export(
            pytorch_model,
            dummy_input,
            "temp.onnx",
            export_params=True,
            opset_version=11,
            do_constant_folding=True,
            input_names=[input_name],
            output_names=["output_0"],
            dynamic_axes={input_name: {0: "batch"}}
        )

    # 解析 ONNX 模型
    print("Parsing ONNX model...")
    parser = trt.OnnxParser(network, logger)
    with open("temp.onnx", "rb") as f:
        if not parser.parse(f.read()):
            for error in range(parser.num_errors):
                print(parser.get_error(error))
            raise RuntimeError("Failed to parse ONNX model")

    # 设置输出
    output = network.get_output(0)
    network.mark_output(output)

    # 构建引擎
    print("Building TensorRT engine...")
    engine = builder.build_engine(network, config)

    return engine


# 3. 保存引擎
def save_engine(engine, engine_path):
    with open(engine_path, "wb") as f:
        f.write(engine.serialize())
    print(f"TensorRT engine saved to {engine_path}")


# 4. 主函数
def main():
    # 配置参数
    weights_path = "/home/test/edgeyolo-main/best.pth"
    input_size = (640, 640)  # (height, width)
    batch_size = 1
    fp16 = True
    int8 = False
    engine_path = "model.engine"

    # 加载 PyTorch 模型
    model = load_pytorch_model(weights_path)

    # 构建 TensorRT 引擎
    engine = build_trt_engine(model, input_size, batch_size, fp16, int8)

    # 保存引擎
    save_engine(engine, engine_path)


if __name__ == "__main__":
    main()