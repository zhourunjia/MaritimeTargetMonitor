import torch
import torch.onnx
import onnx
import onnxruntime
import numpy as np
from PIL import Image
import torchvision.transforms as transforms
import argparse
import os


# 1. 加载PyTorch模型（确保在CPU上）
def load_pytorch_model(model_path, model_class=None):
    """
    加载PyTorch模型
    :param model_path: 模型文件路径
    :param model_class: 模型类（如果无法自动推断）
    """
    # 检查文件是否存在
    if not os.path.exists(model_path):
        raise FileNotFoundError(f"模型文件不存在: {model_path}")

    # 尝试自动推断模型结构（实际使用时需要替换为你的模型类）
    if model_class is None:
        # 这里使用示例模型，实际使用时替换为你的模型类
        from models import edgeyolo  # 示例：YOLOv5的加载方式
        model = edgeyolo(model_path, map_location='cpu')
    else:
        # 使用提供的模型类
        model = model_class()
        # 加载权重 - 关键：使用 map_location='cpu'
        checkpoint = torch.load(model_path, map_location='cpu')

        # 加载状态字典
        if 'state_dict' in checkpoint:
            model.load_state_dict(checkpoint['state_dict'])
        else:
            # 如果检查点直接是模型状态
            model.load_state_dict(checkpoint)

    # 确保模型在 CPU 上
    model = model.cpu()

    # 设置为评估模式
    model.eval()

    print(f"✓ 模型加载成功: {model_path}")
    return model


# 2. 准备输入数据（确保在CPU上）
def prepare_input(image_path=None, input_size=(3, 640, 640)):
    """
    准备输入数据：
    - 如果提供图像路径，加载并预处理图像
    - 否则创建随机输入张量
    """
    if image_path and os.path.exists(image_path):
        # 图像预处理 - 针对640x640输入
        transform = transforms.Compose([
            transforms.Resize((640, 640)),  # 直接缩放到640x640
            transforms.ToTensor(),
            # 根据你的训练预处理设置归一化参数
            transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
        ])

        image = Image.open(image_path).convert('RGB')
        input_tensor = transform(image)
        input_tensor = input_tensor.unsqueeze(0)  # 添加 batch 维度
        print(f"✓ 图像加载和预处理完成: {image_path}")
    else:
        # 创建随机输入张量 - 针对640x640输入
        input_tensor = torch.randn(1, *input_size)
        print("✓ 随机输入张量创建完成")

    # 确保在 CPU 上
    return input_tensor.cpu()


# 3. 确定输入通道数
def determine_input_channels(model, dummy_input):
    """
    尝试确定模型的输入通道数
    """
    try:
        # 尝试进行一次前向传播
        with torch.no_grad():
            output = model(dummy_input)

        # 如果成功，说明通道数正确
        print(f"✓ 输入通道数验证成功: {dummy_input.shape[1]}通道")
        return dummy_input.shape[1]
    except Exception as e:
        print(f"输入通道数可能不正确: {e}")

        # 尝试常见的通道数
        for channels in [3, 1, 4]:
            if channels == dummy_input.shape[1]:
                continue  # 跳过已经尝试过的

            try:
                # 修改通道数后重试
                new_input = torch.randn(1, channels, 640, 640)
                with torch.no_grad():
                    output = model(new_input)
                print(f"✓ 发现正确的输入通道数: {channels}通道")
                return channels
            except:
                continue

        # 如果所有尝试都失败，返回默认值
        print("⚠ 无法确定正确通道数，使用默认值: 3通道")
        return 3


# 4. 转换到ONNX（确保模型和输入都在CPU上）
def convert_to_onnx(pytorch_model, input_tensor, onnx_path, opset_version=12):
    """
    转换PyTorch模型到ONNX格式
    """
    # 双重保险：确保模型和输入都在 CPU 上
    pytorch_model = pytorch_model.cpu()
    input_tensor = input_tensor.cpu()

    print("开始转换模型到ONNX格式...")

    # 导出ONNX模型
    torch.onnx.export(
        pytorch_model,
        input_tensor,
        onnx_path,
        export_params=True,
        opset_version=opset_version,
        do_constant_folding=True,
        input_names=['input'],
        output_names=['output'],
        dynamic_axes={
            'input': {0: 'batch_size'},  # 支持动态batch
            'output': {0: 'batch_size'}
        }
    )

    # 验证ONNX模型格式是否正确
    onnx_model = onnx.load(onnx_path)
    onnx.checker.check_model(onnx_model)
    print(f"✓ ONNX模型已成功导出: {onnx_path}")

    return onnx_model


# 5. 验证转换精度（确保设备一致）
def validate_conversion(pytorch_model, onnx_path, input_tensor):
    """
    验证PyTorch和ONNX模型的输出是否一致
    """
    # 确保输入在 CPU 上
    input_tensor = input_tensor.cpu()

    # PyTorch推理 - 确保模型在 CPU 上
    pytorch_model = pytorch_model.cpu()
    with torch.no_grad():
        pytorch_output = pytorch_model(input_tensor).cpu().numpy()

    # ONNX推理
    ort_session = onnxruntime.InferenceSession(onnx_path)
    ort_inputs = {ort_session.get_inputs()[0].name: input_tensor.numpy()}
    ort_output = ort_session.run(None, ort_inputs)[0]

    # 比较结果
    diff = np.abs(pytorch_output - ort_output).max()
    print(f"PyTorch和ONNX输出最大差异: {diff}")

    if diff > 1e-5:
        print("⚠ 警告: 检测到显著的精度损失!")
        return False

    print("✓ 精度验证通过")
    return True


# 6. 测试实际识别效果
def test_recognition(onnx_path, input_tensor, class_names=None):
    """
    测试ONNX模型的识别效果
    """
    # ONNX推理
    ort_session = onnxruntime.InferenceSession(onnx_path)
    ort_inputs = {ort_session.get_inputs()[0].name: input_tensor.numpy()}
    ort_output = ort_session.run(None, ort_inputs)[0]

    # 获取预测结果
    if ort_output.shape[1] > 1:  # 分类任务
        probabilities = torch.nn.functional.softmax(torch.tensor(ort_output), dim=1)
        top_prob, top_class = probabilities.topk(1, dim=1)

        # 输出结果
        if class_names and len(class_names) > top_class.item():
            print(f"预测结果: {class_names[top_class.item()]}, 置信度: {top_prob.item():.4f}")
        else:
            print(f"预测类别: {top_class.item()}, 置信度: {top_prob.item():.4f}")

        # 检查是否识别失败
        if top_prob.item() < 0.1:  # 置信度过低阈值
            print("⚠ 警告: 模型未能有效识别图像内容!")
            return False
    else:  # 回归或检测任务
        print(f"模型输出: {ort_output}")

    return True


# 主函数
def main():
    parser = argparse.ArgumentParser(description='PyTorch到ONNX模型转换工具')
    parser.add_argument('--model', type=str, required=True, help='PyTorch模型文件路径')
    parser.add_argument('--output', type=str, default='converted_model.onnx', help='输出的ONNX模型路径')
    parser.add_argument('--test_image', type=str, help='用于测试识别效果的图像路径')
    parser.add_argument('--opset', type=int, default=12, help='ONNX opset版本')
    parser.add_argument('--channels', type=int, default=3, help='输入通道数 (1, 3, 4)')

    args = parser.parse_args()

    print("=" * 60)
    print("PyTorch到ONNX模型转换工具 (640x640输入专用)")
    print("=" * 60)

    # 步骤1: 加载PyTorch模型（确保在CPU上）
    print("\n[1/4] 加载PyTorch模型...")
    model = load_pytorch_model(args.model)

    # 步骤2: 准备输入数据（确保在CPU上）
    print("\n[2/4] 准备输入数据...")
    input_size = (args.channels, 640, 640)  # 640x640输入尺寸

    # 创建示例输入（用于转换和精度验证）
    dummy_input = prepare_input(input_size=input_size)

    # 验证通道数是否正确
    actual_channels = determine_input_channels(model, dummy_input)
    if actual_channels != args.channels:
        print(f"更新通道数: {args.channels} -> {actual_channels}")
        input_size = (actual_channels, 640, 640)
        dummy_input = prepare_input(input_size=input_size)

    # 准备真实测试输入（用于功能验证）
    if args.test_image:
        test_input = prepare_input(image_path=args.test_image, input_size=input_size)
    else:
        test_input = dummy_input  # 使用随机输入进行测试
        print("⚠ 未提供测试图像，使用随机输入进行功能测试")

    # 步骤3: 转换为ONNX
    print("\n[3/4] 转换模型到ONNX格式...")
    onnx_model = convert_to_onnx(model, dummy_input, args.output, args.opset)

    # 步骤4: 验证转换结果
    print("\n[4/4] 验证转换结果...")

    print("\n验证转换精度...")
    precision_ok = validate_conversion(model, args.output, dummy_input)

    print("\n测试识别效果...")
    recognition_ok = test_recognition(args.output, test_input)

    print("\n" + "=" * 60)
    if precision_ok and recognition_ok:
        print("✓ 转换成功! ONNX模型保持良好识别能力")
    else:
        print("⚠ 转换完成，但存在一些问题，请检查上述警告")
    print("=" * 60)


if __name__ == "__main__":
    main()