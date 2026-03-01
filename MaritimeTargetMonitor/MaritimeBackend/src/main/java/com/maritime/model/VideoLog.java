package com.maritime.model;

import io.swagger.v3.oas.annotations.media.Schema;

import javax.persistence.*;
import java.time.LocalDateTime;

@Schema(description = "视频日志")
@Entity
@Table(name = "video_log")
public class VideoLog {

    @Schema(description = "主键ID")
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Schema(description = "设备ID")
    @Column(name = "device_id", nullable = false)
    private String deviceId;

    @Schema(description = "视频ID")
    @Column(name = "video_id")
    private Long videoId;

    @Schema(description = "视频名称")
    @Column(name = "video_name")
    private String videoName;

    @Schema(description = "操作类型：record/playback/download/delete")
    @Column(name = "operation_type", nullable = false)
    private String operationType;

    @Schema(description = "操作结果：success/failed")
    @Column(name = "operation_result", nullable = false)
    private String operationResult;

    @Schema(description = "操作时间")
    @Column(name = "operation_time", nullable = false)
    private LocalDateTime operationTime;

    @Schema(description = "操作人")
    @Column(name = "operator")
    private String operator;

    @Schema(description = "操作备注")
    @Column(name = "remark", columnDefinition = "TEXT")
    private String remark;

    @Schema(description = "创建时间")
    @Column(name = "created_at", nullable = false, updatable = false)
    private LocalDateTime createdAt;

    @PrePersist
    protected void onCreate() {
        createdAt = LocalDateTime.now();
        if (operationTime == null) {
            operationTime = LocalDateTime.now();
        }
    }

    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public String getDeviceId() {
        return deviceId;
    }

    public void setDeviceId(String deviceId) {
        this.deviceId = deviceId;
    }

    public Long getVideoId() {
        return videoId;
    }

    public void setVideoId(Long videoId) {
        this.videoId = videoId;
    }

    public String getVideoName() {
        return videoName;
    }

    public void setVideoName(String videoName) {
        this.videoName = videoName;
    }

    public String getOperationType() {
        return operationType;
    }

    public void setOperationType(String operationType) {
        this.operationType = operationType;
    }

    public String getOperationResult() {
        return operationResult;
    }

    public void setOperationResult(String operationResult) {
        this.operationResult = operationResult;
    }

    public LocalDateTime getOperationTime() {
        return operationTime;
    }

    public void setOperationTime(LocalDateTime operationTime) {
        this.operationTime = operationTime;
    }

    public String getOperator() {
        return operator;
    }

    public void setOperator(String operator) {
        this.operator = operator;
    }

    public String getRemark() {
        return remark;
    }

    public void setRemark(String remark) {
        this.remark = remark;
    }

    public LocalDateTime getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(LocalDateTime createdAt) {
        this.createdAt = createdAt;
    }
}