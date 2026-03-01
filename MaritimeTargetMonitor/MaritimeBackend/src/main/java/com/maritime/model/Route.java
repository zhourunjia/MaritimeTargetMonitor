package com.maritime.model;

import io.swagger.v3.oas.annotations.media.Schema;
import javax.persistence.*;
import java.time.LocalDateTime;

@Schema(description = "轨迹")
@Entity
@Table(name = "route")
public class Route {

    @Schema(description = "主键ID")
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Schema(description = "轨迹名称")
    @Column(name = "route_name", nullable = false)
    private String routeName;

    @Schema(description = "设备ID")
    @Column(name = "device_id")
    private String deviceId;

    @Schema(description = "轨迹点列表（JSON格式）")
    @Column(name = "points", columnDefinition = "text")
    private String points;

    @Schema(description = "轨迹描述")
    @Column(name = "description")
    private String description;

    @Schema(description = "创建人")
    @Column(name = "creator")
    private String creator;

    @Schema(description = "创建时间")
    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;

    @Schema(description = "更新时间")
    @Column(name = "updated_at", nullable = false)
    private LocalDateTime updatedAt;

    // Getters and Setters
    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public String getRouteName() {
        return routeName;
    }

    public void setRouteName(String routeName) {
        this.routeName = routeName;
    }

    public String getDeviceId() {
        return deviceId;
    }

    public void setDeviceId(String deviceId) {
        this.deviceId = deviceId;
    }

    public String getPoints() {
        return points;
    }

    public void setPoints(String points) {
        this.points = points;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }

    public String getCreator() {
        return creator;
    }

    public void setCreator(String creator) {
        this.creator = creator;
    }

    public LocalDateTime getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(LocalDateTime createdAt) {
        this.createdAt = createdAt;
    }

    public LocalDateTime getUpdatedAt() {
        return updatedAt;
    }

    public void setUpdatedAt(LocalDateTime updatedAt) {
        this.updatedAt = updatedAt;
    }
}