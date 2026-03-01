package com.maritime.controller;

import com.maritime.service.FileUploadService;
import com.maritime.utils.SResult;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

import javax.servlet.http.HttpServletRequest;
import java.io.IOException;

@Tag(name = "文件上传", description = "文件上传相关接口")
@RestController
@RequestMapping("/app/upload")
public class FileUploadController {

    @Autowired
    private FileUploadService fileUploadService;

    /**
     * 上传电池数据文件
     */
    @Operation(summary = "上传电池数据", description = "上传电池数据文件，支持JSON和文本格式")
    @PostMapping("/battery")
    public SResult<?> uploadBatteryFile(
            @Parameter(description = "上传的文件") @RequestParam("file") MultipartFile file,
            @Parameter(description = "设备ID") @RequestParam(value = "deviceId", required = false) String deviceId,
            HttpServletRequest request) {
        return uploadFile("battery", file, deviceId, request);
    }

    /**
     * 上传环境数据文件
     */
    @Operation(summary = "上传环境数据", description = "上传环境数据文件，支持JSON和文本格式")
    @PostMapping("/environment")
    public SResult<?> uploadEnvironmentFile(
            @Parameter(description = "上传的文件") @RequestParam("file") MultipartFile file,
            @Parameter(description = "设备ID") @RequestParam(value = "deviceId", required = false) String deviceId,
            HttpServletRequest request) {
        return uploadFile("environment", file, deviceId, request);
    }

    /**
     * 上传速度数据文件
     */
    @Operation(summary = "上传速度数据", description = "上传速度数据文件，支持JSON和文本格式")
    @PostMapping("/speed")
    public SResult<?> uploadSpeedFile(
            @Parameter(description = "上传的文件") @RequestParam("file") MultipartFile file,
            @Parameter(description = "设备ID") @RequestParam(value = "deviceId", required = false) String deviceId,
            HttpServletRequest request) {
        return uploadFile("speed", file, deviceId, request);
    }

    /**
     * 上传照片文件
     */
    @Operation(summary = "上传照片", description = "上传照片文件，支持JPEG、PNG、GIF、WebP格式")
    @PostMapping("/photo")
    public SResult<?> uploadPhotoFile(
            @Parameter(description = "上传的文件") @RequestParam("file") MultipartFile file,
            @Parameter(description = "设备ID") @RequestParam(value = "deviceId", required = false) String deviceId,
            HttpServletRequest request) {
        return uploadFile("photo", file, deviceId, request);
    }

    /**
     * 上传轨迹数据文件
     */
    @Operation(summary = "上传轨迹数据", description = "上传轨迹数据文件，支持JSON和文本格式")
    @PostMapping("/trajectory")
    public SResult<?> uploadTrajectoryFile(
            @Parameter(description = "上传的文件") @RequestParam("file") MultipartFile file,
            @Parameter(description = "设备ID") @RequestParam(value = "deviceId", required = false) String deviceId,
            HttpServletRequest request) {
        return uploadFile("trajectory", file, deviceId, request);
    }

    /**
     * 通用文件上传方法
     */
    private SResult<?> uploadFile(String type, MultipartFile file, String deviceId, HttpServletRequest request) {
        try {
            // 获取上传者信息（从JWT token或session中获取）
            String uploader = "system"; // 默认上传者，实际项目中应该从认证信息中获取

            // 调用服务层上传文件
            com.maritime.model.FileMetadata metadata = fileUploadService.uploadFile(type, file, deviceId, uploader);

            // 返回上传结果
            return SResult.success(metadata);
        } catch (IllegalArgumentException e) {
            return SResult.error(400, e.getMessage());
        } catch (IOException e) {
            return SResult.error(500, "文件上传失败：" + e.getMessage());
        } catch (Exception e) {
            return SResult.error(500, "上传失败：" + e.getMessage());
        }
    }

    /**
     * 检查上传目录是否可写
     */
    @Operation(summary = "检查上传目录", description = "检查上传目录是否可写")
    @GetMapping("/check")
    public SResult<?> checkUploadDir() {
        boolean writable = fileUploadService.checkUploadDirWritable();
        if (writable) {
            return SResult.success("上传目录可写");
        } else {
            return SResult.error(500, "上传目录不可写");
        }
    }

    /**
     * 清理过期文件
     */
    @Operation(summary = "清理过期文件", description = "清理指定天数之前的文件")
    @DeleteMapping("/clean")
    public SResult<?> cleanExpiredFiles(
            @Parameter(description = "保留天数") @RequestParam("days") int days) {
        try {
            int count = fileUploadService.cleanExpiredFiles(days);
            return SResult.success("清理了 " + count + " 个过期文件");
        } catch (Exception e) {
            return SResult.error(500, "清理失败：" + e.getMessage());
        }
    }

    /**
     * 删除文件
     */
    @Operation(summary = "删除文件", description = "根据文件ID删除文件")
    @DeleteMapping("/delete/{id}")
    public SResult<?> deleteFile(
            @Parameter(description = "文件ID") @PathVariable("id") Long id) {
        try {
            boolean deleted = fileUploadService.deleteFile(id);
            if (deleted) {
                return SResult.success("文件删除成功");
            } else {
                return SResult.error(404, "文件不存在");
            }
        } catch (Exception e) {
            return SResult.error(500, "删除失败：" + e.getMessage());
        }
    }
}
