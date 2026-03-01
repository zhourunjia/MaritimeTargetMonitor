import xml.etree.ElementTree as ET
import os
import argparse


def xml_to_yolo(xml_path, output_dir, class_map):
    """
    将单个XML标注文件转换为YOLO格式的txt文件

    参数:
    xml_path: XML文件路径
    output_dir: 输出目录路径
    class_map: 类别名称到ID的映射字典
    """
    try:
        # 解析XML文件
        tree = ET.parse(xml_path)
        root = tree.getroot()

        # 获取图像尺寸
        size = root.find('size')
        width = int(size.find('width').text)
        height = int(size.find('height').text)

        # 准备输出内容
        yolo_lines = []

        # 遍历所有目标对象
        for obj in root.findall('object'):
            # 获取类别名称
            cls_name = obj.find('name').text

            # 检查类别是否在映射中
            if cls_name not in class_map:
                print(f"警告: 在文件 {xml_path} 中发现未知类别 '{cls_name}'，已跳过")
                continue

            cls_id = class_map[cls_name]

            # 获取边界框坐标
            bndbox = obj.find('bndbox')
            try:
                xmin = float(bndbox.find('xmin').text)
                ymin = float(bndbox.find('ymin').text)
                xmax = float(bndbox.find('xmax').text)
                ymax = float(bndbox.find('ymax').text)
            except Exception as e:
                print(f"错误: 在文件 {xml_path} 中解析边界框时出错: {e}")
                continue

            # 验证边界框坐标
            if xmin >= xmax or ymin >= ymax:
                print(f"警告: 在文件 {xml_path} 中发现无效边界框 ({xmin},{ymin},{xmax},{ymax})，已跳过")
                continue

            # 计算中心点坐标和宽高（归一化）
            x_center = (xmin + xmax) / (2 * width)
            y_center = (ymin + ymax) / (2 * height)
            w = (xmax - xmin) / width
            h = (ymax - ymin) / height

            # 确保值在0-1范围内
            x_center = max(0, min(1, x_center))
            y_center = max(0, min(1, y_center))
            w = max(0, min(1, w))
            h = max(0, min(1, h))

            # 格式化为YOLO格式
            yolo_line = f"{cls_id} {x_center:.6f} {y_center:.6f} {w:.6f} {h:.6f}"
            yolo_lines.append(yolo_line)

        # 写入输出文件
        if yolo_lines:
            filename = os.path.splitext(os.path.basename(xml_path))[0]
            output_path = os.path.join(output_dir, f"{filename}.txt")
            with open(output_path, 'w') as f:
                f.write("\n".join(yolo_lines))
            return True
        else:
            print(f"警告: 文件 {xml_path} 中没有有效的对象，创建空文件")
            # 创建空文件以保持一致性
            filename = os.path.splitext(os.path.basename(xml_path))[0]
            output_path = os.path.join(output_dir, f"{filename}.txt")
            open(output_path, 'a').close()
            return False

    except Exception as e:
        print(f"错误: 处理文件 {xml_path} 时出错: {e}")
        return False


def batch_convert_xml_to_yolo(xml_dir, output_dir, class_map):
    """
    批量转换目录中的所有XML文件为YOLO格式

    参数:
    xml_dir: 包含XML文件的目录路径
    output_dir: 输出目录路径
    class_map: 类别名称到ID的映射字典
    """
    # 确保输出目录存在
    os.makedirs(output_dir, exist_ok=True)

    # 获取所有XML文件
    xml_files = [f for f in os.listdir(xml_dir) if f.endswith('.xml')]

    if not xml_files:
        print(f"错误: 在目录 {xml_dir} 中没有找到XML文件")
        return

    print(f"找到 {len(xml_files)} 个XML文件，开始转换...")

    success_count = 0
    for filename in xml_files:
        xml_path = os.path.join(xml_dir, filename)
        if xml_to_yolo(xml_path, output_dir, class_map):
            success_count += 1

    print(f"转换完成! 成功转换 {success_count}/{len(xml_files)} 个文件")
    print(f"输出保存在: {output_dir}")


def main():
    # 设置命令行参数
    parser = argparse.ArgumentParser(description='批量将XML标注转换为YOLO格式')
    parser.add_argument('--xml_dir', type=str, required=True, help='包含XML文件的目录路径')
    parser.add_argument('--output_dir', type=str, required=True, help='输出目录路径')
    parser.add_argument('--class_map', type=str, required=True,
                        help='类别映射，格式为 "class1:id1,class2:id2,..."')

    args = parser.parse_args()

    # 解析类别映射
    try:
        class_map = {}
        for item in args.class_map.split(','):
            cls_name, cls_id = item.split(':')
            class_map[cls_name.strip()] = int(cls_id.strip())
        print(f"使用类别映射: {class_map}")
    except Exception as e:
        print(f"错误: 解析类别映射失败: {e}")
        print('请使用格式: "class1:id1,class2:id2,..."')
        return

    # 执行批量转换
    batch_convert_xml_to_yolo(args.xml_dir, args.output_dir, class_map)


if __name__ == "__main__":
    main()