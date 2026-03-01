package com.maritime.model;

import io.swagger.v3.oas.annotations.media.Schema;

import javax.persistence.*;
import java.time.LocalDateTime;

@Schema(description = "机器人运行日志")
@Entity
@Table(name = "robot_run_log")
public class RobotRunLog {

    @Schema(description = "主键ID")
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Schema(description = "设备ID")
    @Column(name = "device_id", nullable = false)
    private String deviceId;

    @Schema(description = "任务ID")
    @Column(name = "task_id")
    private String taskId;

    @Schema(description = "任务名称")
    @Column(name = "task_name")
    private String taskName;

    @Schema(description = "运行状态：running/paused/stopped/completed/failed")
    @Column(name = "run_status", nullable = false)
    private String runStatus;

    @Schema(description = "开始时间")
    @Column(name = "start_time")
    private LocalDateTime startTime;

    @Schema(description = "结束时间")
    @Column(name = "end_time")
    private LocalDateTime endTime;

    @Schema(description = "运行时长（秒）")
    @Column(name = "duration")
    private Integer duration;

    @Schema(description = "运行距离（米）")
    @Column(name = "distance")
    private Double distance;

    @Schema(description = "运行内容")
    @Column(name = "run_content", columnDefinition = "TEXT")
    private String runContent;

    @Schema(description = "创建时间")
    @Column(name = "created_at", nullable = false, updatable = false)
    private LocalDateTime createdAt;

    @PrePersist
    protected void onCreate() {
        createdAt = LocalDateTime.now();
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

    public String getTaskId() {
        return taskId;
    }

    public void setTaskId(String taskId) {
        this.taskId = taskId;
    }

    public String getTaskName() {
        return taskName;
    }

    public void setTaskName(String taskName) {
        this.taskName = taskName;
    }

    public String getRunStatus() {
        return runStatus;
    }

    public void setRunStatus(String runStatus) {
        this.runStatus = runStatus;
    }

    public LocalDateTime getStartTime() {
        return startTime;
    }

    public void setStartTime(LocalDateTime startTime) {
        this.startTime = startTime;
    }

    public LocalDateTime getEndTime() {
        return endTime;
    }

    public void setEndTime(LocalDateTime endTime) {
        this.endTime = endTime;
    }

    public Integer getDuration() {
        return duration;
    }

    public void setDuration(Integer duration) {
        this.duration = duration;
    }

    public Double getDistance() {
        return distance;
    }

    public void setDistance(Double distance) {
        this.distance = distance;
    }

    public String getRunContent() {
        return runContent;
    }

    public void setRunContent(String runContent) {
        this.runContent = runContent;
    }

    public LocalDateTime getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(LocalDateTime createdAt) {
        this.createdAt = createdAt;
    }
}