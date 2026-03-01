package com.maritime.model;

import io.swagger.v3.oas.annotations.media.Schema;

import javax.persistence.*;
import java.time.LocalDateTime;

@Schema(description = "报警日志")
@Entity
@Table(name = "alarm_log")
public class AlarmLog {

    @Schema(description = "主键ID")
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Schema(description = "设备ID")
    @Column(name = "device_id", nullable = false)
    private String deviceId;

    @Schema(description = "报警类型")
    @Column(name = "alarm_type", nullable = false)
    private String alarmType;

    @Schema(description = "报警级别：info/warning/error/critical")
    @Column(name = "alarm_level", nullable = false)
    private String alarmLevel;

    @Schema(description = "报警内容")
    @Column(name = "alarm_content", nullable = false, columnDefinition = "TEXT")
    private String alarmContent;

    @Schema(description = "报警时间")
    @Column(name = "alarm_time", nullable = false)
    private LocalDateTime alarmTime;

    @Schema(description = "是否已处理")
    @Column(name = "is_handled")
    private Boolean isHandled = false;

    @Schema(description = "处理时间")
    @Column(name = "handle_time")
    private LocalDateTime handleTime;

    @Schema(description = "处理人")
    @Column(name = "handler")
    private String handler;

    @Schema(description = "处理备注")
    @Column(name = "handle_remark", columnDefinition = "TEXT")
    private String handleRemark;

    @Schema(description = "创建时间")
    @Column(name = "created_at", nullable = false, updatable = false)
    private LocalDateTime createdAt;

    @PrePersist
    protected void onCreate() {
        createdAt = LocalDateTime.now();
        if (alarmTime == null) {
            alarmTime = LocalDateTime.now();
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

    public String getAlarmType() {
        return alarmType;
    }

    public void setAlarmType(String alarmType) {
        this.alarmType = alarmType;
    }

    public String getAlarmLevel() {
        return alarmLevel;
    }

    public void setAlarmLevel(String alarmLevel) {
        this.alarmLevel = alarmLevel;
    }

    public String getAlarmContent() {
        return alarmContent;
    }

    public void setAlarmContent(String alarmContent) {
        this.alarmContent = alarmContent;
    }

    public LocalDateTime getAlarmTime() {
        return alarmTime;
    }

    public void setAlarmTime(LocalDateTime alarmTime) {
        this.alarmTime = alarmTime;
    }

    public Boolean getIsHandled() {
        return isHandled;
    }

    public void setIsHandled(Boolean isHandled) {
        this.isHandled = isHandled;
    }

    public LocalDateTime getHandleTime() {
        return handleTime;
    }

    public void setHandleTime(LocalDateTime handleTime) {
        this.handleTime = handleTime;
    }

    public String getHandler() {
        return handler;
    }

    public void setHandler(String handler) {
        this.handler = handler;
    }

    public String getHandleRemark() {
        return handleRemark;
    }

    public void setHandleRemark(String handleRemark) {
        this.handleRemark = handleRemark;
    }

    public LocalDateTime getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(LocalDateTime createdAt) {
        this.createdAt = createdAt;
    }
}