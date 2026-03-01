package com.maritime.model;

import io.swagger.v3.oas.annotations.media.Schema;

import javax.persistence.*;
import java.time.LocalDateTime;

@Schema(description = "审计日志")
@Entity
@Table(name = "audit_log")
public class AuditLog {

    @Schema(description = "主键ID")
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Schema(description = "操作人")
    @Column(name = "operator", nullable = false)
    private String operator;

    @Schema(description = "操作类型")
    @Column(name = "operation_type", nullable = false)
    private String operationType;

    @Schema(description = "操作模块")
    @Column(name = "module")
    private String module;

    @Schema(description = "操作方法")
    @Column(name = "method")
    private String method;

    @Schema(description = "操作参数")
    @Column(name = "params", columnDefinition = "TEXT")
    private String params;

    @Schema(description = "操作结果：success/failed")
    @Column(name = "result")
    private String result;

    @Schema(description = "操作结果消息")
    @Column(name = "result_message", columnDefinition = "TEXT")
    private String resultMessage;

    @Schema(description = "操作IP")
    @Column(name = "ip_address")
    private String ipAddress;

    @Schema(description = "操作时间")
    @Column(name = "operation_time", nullable = false)
    private LocalDateTime operationTime;

    @Schema(description = "耗时（毫秒）")
    @Column(name = "duration")
    private Long duration;

    @PrePersist
    protected void onCreate() {
        operationTime = LocalDateTime.now();
    }

    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public String getOperator() {
        return operator;
    }

    public void setOperator(String operator) {
        this.operator = operator;
    }

    public String getOperationType() {
        return operationType;
    }

    public void setOperationType(String operationType) {
        this.operationType = operationType;
    }

    public String getModule() {
        return module;
    }

    public void setModule(String module) {
        this.module = module;
    }

    public String getMethod() {
        return method;
    }

    public void setMethod(String method) {
        this.method = method;
    }

    public String getParams() {
        return params;
    }

    public void setParams(String params) {
        this.params = params;
    }

    public String getResult() {
        return result;
    }

    public void setResult(String result) {
        this.result = result;
    }

    public String getResultMessage() {
        return resultMessage;
    }

    public void setResultMessage(String resultMessage) {
        this.resultMessage = resultMessage;
    }

    public String getIpAddress() {
        return ipAddress;
    }

    public void setIpAddress(String ipAddress) {
        this.ipAddress = ipAddress;
    }

    public LocalDateTime getOperationTime() {
        return operationTime;
    }

    public void setOperationTime(LocalDateTime operationTime) {
        this.operationTime = operationTime;
    }

    public Long getDuration() {
        return duration;
    }

    public void setDuration(Long duration) {
        this.duration = duration;
    }
}