package com.maritime.model;

import io.swagger.v3.oas.annotations.media.Schema;

import javax.persistence.*;
import java.time.LocalDateTime;

@Schema(description = "机器人历史记录")
@Entity
@Table(name = "robot_history")
public class RobotHistory {

    @Schema(description = "主键ID")
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Schema(description = "设备ID")
    @Column(name = "device_id", nullable = false)
    private String deviceId;

    @Schema(description = "设备名称")
    @Column(name = "device_name")
    private String deviceName;

    @Schema(description = "任务ID")
    @Column(name = "task_id")
    private String taskId;

    @Schema(description = "任务名称")
    @Column(name = "task_name")
    private String taskName;

    @Schema(description = "任务类型")
    @Column(name = "task_type")
    private String taskType;

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

    @Schema(description = "运行状态：success/failed/interrupted")
    @Column(name = "run_status")
    private String runStatus;

    @Schema(description = "起始位置X")
    @Column(name = "start_x")
    private Double startX;

    @Schema(description = "起始位置Y")
    @Column(name = "start_y")
    private Double startY;

    @Schema(description = "结束位置X")
    @Column(name = "end_x")
    private Double endX;

    @Schema(description = "结束位置Y")
    @Column(name = "end_y")
    private Double endY;

    @Schema(description = "轨迹数据（JSON格式）")
    @Column(name = "trajectory_data", columnDefinition = "TEXT")
    private String trajectoryData;

    @Schema(description = "备注")
    @Column(name = "remark", columnDefinition = "TEXT")
    private String remark;

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

    public String getDeviceName() {
        return deviceName;
    }

    public void setDeviceName(String deviceName) {
        this.deviceName = deviceName;
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

    public String getTaskType() {
        return taskType;
    }

    public void setTaskType(String taskType) {
        this.taskType = taskType;
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

    public String getRunStatus() {
        return runStatus;
    }

    public void setRunStatus(String runStatus) {
        this.runStatus = runStatus;
    }

    public Double getStartX() {
        return startX;
    }

    public void setStartX(Double startX) {
        this.startX = startX;
    }

    public Double getStartY() {
        return startY;
    }

    public void setStartY(Double startY) {
        this.startY = startY;
    }

    public Double getEndX() {
        return endX;
    }

    public void setEndX(Double endX) {
        this.endX = endX;
    }

    public Double getEndY() {
        return endY;
    }

    public void setEndY(Double endY) {
        this.endY = endY;
    }

    public String getTrajectoryData() {
        return trajectoryData;
    }

    public void setTrajectoryData(String trajectoryData) {
        this.trajectoryData = trajectoryData;
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