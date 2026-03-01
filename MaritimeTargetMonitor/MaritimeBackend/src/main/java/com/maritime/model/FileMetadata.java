package com.maritime.model;

import io.swagger.v3.oas.annotations.media.Schema;
import javax.persistence.*;
import java.time.LocalDateTime;

@Schema(description = "文件元数据")
@Entity
@Table(name = "file_metadata")
public class FileMetadata {

    @Schema(description = "主键ID")
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Schema(description = "文件类型：battery/environment/speed/photo/trajectory")
    @Column(name = "type", nullable = false, length = 50)
    private String type;

    @Schema(description = "原始文件名")
    @Column(name = "filename", nullable = false, length = 255)
    private String filename;

    @Schema(description = "存储路径")
    @Column(name = "filepath", nullable = false, length = 500)
    private String filepath;

    @Schema(description = "文件大小（字节）")
    @Column(name = "filesize")
    private Long filesize;

    @Schema(description = "文件MIME类型")
    @Column(name = "filetype", length = 100)
    private String filetype;

    @Schema(description = "关联的设备ID")
    @Column(name = "device_id", length = 100)
    private String deviceId;

    @Schema(description = "上传时间")
    @Column(name = "upload_time", nullable = false)
    private LocalDateTime uploadTime;

    @Schema(description = "上传者")
    @Column(name = "uploader", length = 100)
    private String uploader;

    @Schema(description = "文件MD5值")
    @Column(name = "md5", length = 32)
    private String md5;

    // Getters and Setters
    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public String getType() {
        return type;
    }

    public void setType(String type) {
        this.type = type;
    }

    public String getFilename() {
        return filename;
    }

    public void setFilename(String filename) {
        this.filename = filename;
    }

    public String getFilepath() {
        return filepath;
    }

    public void setFilepath(String filepath) {
        this.filepath = filepath;
    }

    public Long getFilesize() {
        return filesize;
    }

    public void setFilesize(Long filesize) {
        this.filesize = filesize;
    }

    public String getFiletype() {
        return filetype;
    }

    public void setFiletype(String filetype) {
        this.filetype = filetype;
    }

    public String getDeviceId() {
        return deviceId;
    }

    public void setDeviceId(String deviceId) {
        this.deviceId = deviceId;
    }

    public LocalDateTime getUploadTime() {
        return uploadTime;
    }

    public void setUploadTime(LocalDateTime uploadTime) {
        this.uploadTime = uploadTime;
    }

    public String getUploader() {
        return uploader;
    }

    public void setUploader(String uploader) {
        this.uploader = uploader;
    }

    public String getMd5() {
        return md5;
    }

    public void setMd5(String md5) {
        this.md5 = md5;
    }
}