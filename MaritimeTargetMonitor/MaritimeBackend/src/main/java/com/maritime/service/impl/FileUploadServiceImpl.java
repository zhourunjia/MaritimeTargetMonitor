package com.maritime.service.impl;

import com.maritime.config.FileUploadConfig;
import com.maritime.model.FileMetadata;
import com.maritime.repository.FileMetadataRepository;
import com.maritime.service.FileUploadService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.UUID;

@Service
public class FileUploadServiceImpl implements FileUploadService {

    @Autowired
    private FileUploadConfig fileUploadConfig;

    @Autowired
    private FileMetadataRepository fileMetadataRepository;

    @Override
    public FileMetadata uploadFile(String type, MultipartFile file, String deviceId, String uploader) throws IOException {
        // 1. 验证文件类型
        validateFileType(type, file);

        // 2. 验证文件大小
        validateFileSize(file);

        // 3. 生成唯一文件名
        String originalFilename = file.getOriginalFilename();
        String extension = getFileExtension(originalFilename);
        String uniqueFilename = generateUniqueFilename(type, extension);

        // 4. 构建存储路径
        String relativePath = buildRelativePath(type);
        String fullPath = buildFullPath(relativePath, uniqueFilename);

        // 5. 创建上传目录
        createUploadDir(fullPath);

        // 6. 保存文件到磁盘
        saveFileToDisk(file, fullPath);

        // 7. 计算文件MD5值
        String md5 = calculateFileMd5(fullPath);

        // 8. 保存文件元数据到数据库
        FileMetadata metadata = new FileMetadata();
        metadata.setType(type);
        metadata.setFilename(originalFilename);
        metadata.setFilepath(relativePath + File.separator + uniqueFilename);
        metadata.setFilesize(file.getSize());
        metadata.setFiletype(file.getContentType());
        metadata.setDeviceId(deviceId);
        metadata.setUploadTime(LocalDateTime.now());
        metadata.setUploader(uploader);
        metadata.setMd5(md5);

        return fileMetadataRepository.save(metadata);
    }

    @Override
    public boolean checkUploadDirWritable() {
        try {
            String baseDir = fileUploadConfig.getBaseDir();
            Path dirPath = Paths.get(baseDir);

            // 创建目录（如果不存在）
            if (!Files.exists(dirPath)) {
                Files.createDirectories(dirPath);
            }

            // 检查目录是否可写
            File testFile = new File(dirPath.toFile(), ".test_writable.txt");
            boolean writable = testFile.createNewFile();
            if (writable) {
                testFile.delete();
            }
            return writable;
        } catch (Exception e) {
            return false;
        }
    }

    @Override
    public int cleanExpiredFiles(int days) {
        // 实现清理过期文件的逻辑
        // 这里简单返回0，实际项目中需要根据上传时间清理过期文件
        return 0;
    }

    @Override
    public boolean deleteFile(Long id) {
        FileMetadata metadata = fileMetadataRepository.findById(id).orElse(null);
        if (metadata == null) {
            return false;
        }

        // 删除磁盘上的文件
        String fullPath = Paths.get(fileUploadConfig.getBaseDir(), metadata.getFilepath()).toString();
        File file = new File(fullPath);
        if (file.exists()) {
            file.delete();
        }

        // 删除数据库中的元数据
        fileMetadataRepository.delete(metadata);
        return true;
    }

    /**
     * 验证文件类型和扩展名
     */
    private void validateFileType(String type, MultipartFile file) {
        String originalFilename = file.getOriginalFilename();
        String extension = getFileExtension(originalFilename).toLowerCase();
        String contentType = file.getContentType();

        // 检查文件类型是否在允许列表中
        if (!fileUploadConfig.getAllowedTypes(type).contains(contentType)) {
            throw new IllegalArgumentException("不支持的文件类型: " + contentType);
        }

        // 检查文件扩展名是否在允许列表中
        if (!fileUploadConfig.getAllowedExtensions(type).contains(extension)) {
            throw new IllegalArgumentException("不支持的文件扩展名: " + extension);
        }
    }

    /**
     * 验证文件大小
     */
    private void validateFileSize(MultipartFile file) {
        if (file.getSize() > fileUploadConfig.getMaxSizeInBytes()) {
            throw new IllegalArgumentException("文件大小超过限制: " + file.getSize() + " > " + fileUploadConfig.getMaxSizeInBytes());
        }
    }

    /**
     * 获取文件扩展名
     */
    private String getFileExtension(String filename) {
        if (filename == null || !filename.contains(".")) {
            return "";
        }
        return filename.substring(filename.lastIndexOf(".") + 1);
    }

    /**
     * 生成唯一的文件名
     */
    private String generateUniqueFilename(String type, String extension) {
        String timestamp = LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMddHHmmss"));
        String uuid = UUID.randomUUID().toString().replace("-", "");
        return type + "_" + timestamp + "_" + uuid + ("." + extension);
    }

    /**
     * 构建相对路径
     */
    private String buildRelativePath(String type) {
        String datePath = LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyy/MM/dd"));
        return type + File.separator + datePath;
    }

    /**
     * 构建完整路径
     */
    private String buildFullPath(String relativePath, String filename) {
        // 防止路径穿越攻击
        if (filename.contains("../") || filename.contains("..\\")) {
            throw new IllegalArgumentException("文件名包含非法字符");
        }

        return Paths.get(fileUploadConfig.getBaseDir(), relativePath, filename).toString();
    }

    /**
     * 创建上传目录
     */
    private void createUploadDir(String fullPath) throws IOException {
        File file = new File(fullPath);
        File parentDir = file.getParentFile();
        if (!parentDir.exists()) {
            parentDir.mkdirs();
        }
    }

    /**
     * 保存文件到磁盘
     */
    private void saveFileToDisk(MultipartFile file, String fullPath) throws IOException {
        try (InputStream inputStream = file.getInputStream();
             OutputStream outputStream = new FileOutputStream(fullPath)) {
            byte[] buffer = new byte[1024];
            int bytesRead;
            while ((bytesRead = inputStream.read(buffer)) != -1) {
                outputStream.write(buffer, 0, bytesRead);
            }
        }
    }

    /**
     * 计算文件MD5值
     */
    private String calculateFileMd5(String filePath) {
        try (InputStream inputStream = new FileInputStream(filePath)) {
            MessageDigest md = MessageDigest.getInstance("MD5");
            byte[] buffer = new byte[1024];
            int bytesRead;
            while ((bytesRead = inputStream.read(buffer)) != -1) {
                md.update(buffer, 0, bytesRead);
            }
            byte[] digest = md.digest();
            StringBuilder sb = new StringBuilder();
            for (byte b : digest) {
                sb.append(String.format("%02x", b));
            }
            return sb.toString();
        } catch (NoSuchAlgorithmException | IOException e) {
            return null;
        }
    }
}
