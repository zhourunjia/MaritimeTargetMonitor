package com.maritime.config;

import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;

import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Component
@ConfigurationProperties(prefix = "file.upload")
public class FileUploadConfig {

    private String baseDir = "uploads";
    
    private long maxSize = 50; // 默认50MB
    
    private Map<String, List<String>> allowedTypes = new HashMap<>();
    
    private Map<String, List<String>> allowedExtensions = new HashMap<>();

    public String getBaseDir() {
        return baseDir;
    }

    public void setBaseDir(String baseDir) {
        this.baseDir = baseDir;
    }

    public long getMaxSize() {
        return maxSize;
    }

    public void setMaxSize(long maxSize) {
        this.maxSize = maxSize;
    }

    public Map<String, List<String>> getAllowedTypes() {
        return allowedTypes;
    }

    public void setAllowedTypes(Map<String, List<String>> allowedTypes) {
        this.allowedTypes = allowedTypes;
    }

    public Map<String, List<String>> getAllowedExtensions() {
        return allowedExtensions;
    }

    public void setAllowedExtensions(Map<String, List<String>> allowedExtensions) {
        this.allowedExtensions = allowedExtensions;
    }

    // 获取指定类型的允许文件类型
    public List<String> getAllowedTypes(String type) {
        return allowedTypes.getOrDefault(type, Collections.emptyList());
    }

    // 获取指定类型的允许文件扩展名
    public List<String> getAllowedExtensions(String type) {
        return allowedExtensions.getOrDefault(type, Collections.emptyList());
    }

    // 获取文件大小限制（字节）
    public long getMaxSizeInBytes() {
        return maxSize * 1024 * 1024;
    }
}
