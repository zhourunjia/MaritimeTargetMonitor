import torch
from edgeyolo.models import EdgeYOLO


def analyze_model(weight_path):
    """分析PyTorch模型结构"""
    print(f"Loading model from {weight_path}")
    model = EdgeYOLO(weights=weight_path).model
    model.eval()

    # 打印模型结构
    print("\nModel architecture:")
    print(model)

    # 打印输出层信息
    print("\nOutput layers:")
    for name, module in model.named_modules():
        if isinstance(module, torch.nn.Conv2d) and module.out_channels == 13 * 3:  # 假设每个锚点13个属性
            print(f"- {name}: {module}")

    # 创建测试输入
    dummy_input = torch.randn(1, 3, 640, 640)

    # 运行推理
    with torch.no_grad():
        output = model(dummy_input)
        print(f"\nOutput shape: {output.shape}")
        print(f"Output range: min={output.min().item():.6f}, max={output.max().item():.6f}")
        print(f"Output mean: {output.mean().item():.6f}, std: {output.std().item():.6f}")

    # 应用Sigmoid查看激活后范围
    output_sigmoid = torch.sigmoid(output)
    print("\nAfter Sigmoid:")
    print(f"Output range: min={output_sigmoid.min().item():.6f}, max={output_sigmoid.max().item():.6f}")

    return model


if __name__ == "__main__":
    weight_path = "best.pth"
    model = analyze_model(weight_path)