package com.maritime.model;

import io.swagger.v3.oas.annotations.media.Schema;

import javax.persistence.*;
import java.time.LocalDateTime;

@Schema(description = "环境日志")
@Entity
@Table(name = "environment_log")
public class EnvironmentLog {

    @Schema(description = "主键ID")
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Schema(description = "设备ID")
    @Column(name = "device_id", nullable = false)
    private String deviceId;

    @Schema(description = "温度（℃）")
    @Column(name = "temperature")
    private Double temperature;

    @Schema(description = "湿度（%）")
    @Column(name = "humidity")
    private Double humidity;

    @Schema(description = "气压（Pa）")
    @Column(name = "pressure")
    private Double pressure;

    @Schema(description = "风速（m/s）")
    @Column(name = "wind_speed")
    private Double windSpeed;

    @Schema(description = "风向")
    @Column(name = "wind_direction")
    private String windDirection;

    @Schema(description = "空气质量指数")
    @Column(name = "air_quality_index")
    private Integer airQualityIndex;

    @Schema(description = "PM2.5")
    @Column(name = "pm25")
    private Double pm25;

    @Schema(description = "PM10")
    @Column(name = "pm10")
    private Double pm10;

    @Schema(description = "记录时间")
    @Column(name = "record_time", nullable = false)
    private LocalDateTime recordTime;

    @Schema(description = "备注")
    @Column(name = "remark", columnDefinition = "TEXT")
    private String remark;

    @Schema(description = "创建时间")
    @Column(name = "created_at", nullable = false, updatable = false)
    private LocalDateTime createdAt;

    @PrePersist
    protected void onCreate() {
        createdAt = LocalDateTime.now();
        if (recordTime == null) {
            recordTime = LocalDateTime.now();
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

    public Double getTemperature() {
        return temperature;
    }

    public void setTemperature(Double temperature) {
        this.temperature = temperature;
    }

    public Double getHumidity() {
        return humidity;
    }

    public void setHumidity(Double humidity) {
        this.humidity = humidity;
    }

    public Double getPressure() {
        return pressure;
    }

    public void setPressure(Double pressure) {
        this.pressure = pressure;
    }

    public Double getWindSpeed() {
        return windSpeed;
    }

    public void setWindSpeed(Double windSpeed) {
        this.windSpeed = windSpeed;
    }

    public String getWindDirection() {
        return windDirection;
    }

    public void setWindDirection(String windDirection) {
        this.windDirection = windDirection;
    }

    public Integer getAirQualityIndex() {
        return airQualityIndex;
    }

    public void setAirQualityIndex(Integer airQualityIndex) {
        this.airQualityIndex = airQualityIndex;
    }

    public Double getPm25() {
        return pm25;
    }

    public void setPm25(Double pm25) {
        this.pm25 = pm25;
    }

    public Double getPm10() {
        return pm10;
    }

    public void setPm10(Double pm10) {
        this.pm10 = pm10;
    }

    public LocalDateTime getRecordTime() {
        return recordTime;
    }

    public void setRecordTime(LocalDateTime recordTime) {
        this.recordTime = recordTime;
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