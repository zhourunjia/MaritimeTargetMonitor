package com.maritime.controller;

import com.maritime.dto.PageRequest;
import com.maritime.dto.PageResponse;
import com.maritime.dto.SResult;
import com.maritime.model.AlarmLog;
import com.maritime.model.EnvironmentLog;
import com.maritime.model.RobotRunLog;
import com.maritime.model.VideoLog;
import com.maritime.repository.AlarmLogRepository;
import com.maritime.repository.EnvironmentLogRepository;
import com.maritime.repository.RobotRunLogRepository;
import com.maritime.repository.VideoLogRepository;
import com.maritime.utils.ResponseUtil;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDateTime;
import java.util.List;

@Tag(name = "日志管理")
@RestController
@RequestMapping("/app/log")
public class LogController {

    @Autowired
    private AlarmLogRepository alarmLogRepository;

    @Autowired
    private RobotRunLogRepository robotRunLogRepository;

    @Autowired
    private EnvironmentLogRepository environmentLogRepository;

    @Autowired
    private VideoLogRepository videoLogRepository;

    @Operation(summary = "分页查询报警日志")
    @PostMapping("/alarm/list")
    public SResult<PageResponse<AlarmLog>> alarmList(
            @Parameter(description = "分页请求") @RequestBody PageRequest pageRequest,
            @Parameter(description = "设备ID") @RequestParam(required = false) String deviceId,
            @Parameter(description = "报警类型") @RequestParam(required = false) String alarmType,
            @Parameter(description = "报警级别") @RequestParam(required = false) String alarmLevel,
            @Parameter(description = "开始时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime startTime,
            @Parameter(description = "结束时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime endTime,
            @Parameter(description = "关键字") @RequestParam(required = false) String keyword) {

        try {
            List<AlarmLog> records = alarmLogRepository.findByConditions(deviceId, alarmType, alarmLevel, startTime, endTime, keyword);
            Long total = alarmLogRepository.countByConditions(deviceId, alarmType, alarmLevel, startTime, endTime, keyword);

            int offset = pageRequest.getOffset();
            int pageSize = pageRequest.getPageSize();

            List<AlarmLog> pageRecords = records.stream()
                    .skip(offset)
                    .limit(pageSize)
                    .toList();

            PageResponse<AlarmLog> pageResponse = PageResponse.of(pageRecords, total, pageRequest.getPageNum(), pageSize);
            return ResponseUtil.success(pageResponse);
        } catch (Exception e) {
            return ResponseUtil.fail("查询失败: " + e.getMessage());
        }
    }

    @Operation(summary = "写入报警日志")
    @PostMapping("/alarm/add")
    public SResult<String> alarmAdd(@Parameter(description = "报警日志") @RequestBody AlarmLog alarmLog) {
        try {
            alarmLogRepository.save(alarmLog);
            return ResponseUtil.success("添加成功");
        } catch (Exception e) {
            return ResponseUtil.fail("添加失败: " + e.getMessage());
        }
    }

    @Operation(summary = "处理报警")
    @PutMapping("/alarm/handle/{id}")
    public SResult<String> alarmHandle(
            @Parameter(description = "报警ID") @PathVariable Long id,
            @Parameter(description = "处理人") @RequestParam String handler,
            @Parameter(description = "处理备注") @RequestParam(required = false) String handleRemark) {

        try {
            AlarmLog alarmLog = alarmLogRepository.findById(id)
                    .orElseThrow(() -> new RuntimeException("报警日志不存在"));

            alarmLog.setIsHandled(true);
            alarmLog.setHandleTime(LocalDateTime.now());
            alarmLog.setHandler(handler);
            alarmLog.setHandleRemark(handleRemark);

            alarmLogRepository.save(alarmLog);
            return ResponseUtil.success("处理成功");
        } catch (Exception e) {
            return ResponseUtil.fail("处理失败: " + e.getMessage());
        }
    }

    @Operation(summary = "分页查询机器人运行日志")
    @PostMapping("/robot-run/list")
    public SResult<PageResponse<RobotRunLog>> robotRunList(
            @Parameter(description = "分页请求") @RequestBody PageRequest pageRequest,
            @Parameter(description = "设备ID") @RequestParam(required = false) String deviceId,
            @Parameter(description = "任务ID") @RequestParam(required = false) String taskId,
            @Parameter(description = "运行状态") @RequestParam(required = false) String runStatus,
            @Parameter(description = "开始时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime startTime,
            @Parameter(description = "结束时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime endTime,
            @Parameter(description = "关键字") @RequestParam(required = false) String keyword) {

        try {
            List<RobotRunLog> records = robotRunLogRepository.findByConditions(deviceId, taskId, runStatus, startTime, endTime, keyword);
            Long total = robotRunLogRepository.countByConditions(deviceId, taskId, runStatus, startTime, endTime, keyword);

            int offset = pageRequest.getOffset();
            int pageSize = pageRequest.getPageSize();

            List<RobotRunLog> pageRecords = records.stream()
                    .skip(offset)
                    .limit(pageSize)
                    .toList();

            PageResponse<RobotRunLog> pageResponse = PageResponse.of(pageRecords, total, pageRequest.getPageNum(), pageSize);
            return ResponseUtil.success(pageResponse);
        } catch (Exception e) {
            return ResponseUtil.fail("查询失败: " + e.getMessage());
        }
    }

    @Operation(summary = "写入机器人运行日志")
    @PostMapping("/robot-run/add")
    public SResult<String> robotRunAdd(@Parameter(description = "机器人运行日志") @RequestBody RobotRunLog robotRunLog) {
        try {
            robotRunLogRepository.save(robotRunLog);
            return ResponseUtil.success("添加成功");
        } catch (Exception e) {
            return ResponseUtil.fail("添加失败: " + e.getMessage());
        }
    }

    @Operation(summary = "分页查询环境日志")
    @PostMapping("/environment/list")
    public SResult<PageResponse<EnvironmentLog>> environmentList(
            @Parameter(description = "分页请求") @RequestBody PageRequest pageRequest,
            @Parameter(description = "设备ID") @RequestParam(required = false) String deviceId,
            @Parameter(description = "开始时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime startTime,
            @Parameter(description = "结束时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime endTime,
            @Parameter(description = "关键字") @RequestParam(required = false) String keyword) {

        try {
            List<EnvironmentLog> records = environmentLogRepository.findByConditions(deviceId, startTime, endTime, keyword);
            Long total = environmentLogRepository.countByConditions(deviceId, startTime, endTime, keyword);

            int offset = pageRequest.getOffset();
            int pageSize = pageRequest.getPageSize();

            List<EnvironmentLog> pageRecords = records.stream()
                    .skip(offset)
                    .limit(pageSize)
                    .toList();

            PageResponse<EnvironmentLog> pageResponse = PageResponse.of(pageRecords, total, pageRequest.getPageNum(), pageSize);
            return ResponseUtil.success(pageResponse);
        } catch (Exception e) {
            return ResponseUtil.fail("查询失败: " + e.getMessage());
        }
    }

    @Operation(summary = "写入环境日志")
    @PostMapping("/environment/add")
    public SResult<String> environmentAdd(@Parameter(description = "环境日志") @RequestBody EnvironmentLog environmentLog) {
        try {
            environmentLogRepository.save(environmentLog);
            return ResponseUtil.success("添加成功");
        } catch (Exception e) {
            return ResponseUtil.fail("添加失败: " + e.getMessage());
        }
    }

    @Operation(summary = "分页查询视频日志")
    @PostMapping("/video/list")
    public SResult<PageResponse<VideoLog>> videoList(
            @Parameter(description = "分页请求") @RequestBody PageRequest pageRequest,
            @Parameter(description = "设备ID") @RequestParam(required = false) String deviceId,
            @Parameter(description = "视频ID") @RequestParam(required = false) Long videoId,
            @Parameter(description = "操作类型") @RequestParam(required = false) String operationType,
            @Parameter(description = "操作结果") @RequestParam(required = false) String operationResult,
            @Parameter(description = "开始时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime startTime,
            @Parameter(description = "结束时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime endTime,
            @Parameter(description = "关键字") @RequestParam(required = false) String keyword) {

        try {
            List<VideoLog> records = videoLogRepository.findByConditions(deviceId, videoId, operationType, operationResult, startTime, endTime, keyword);
            Long total = videoLogRepository.countByConditions(deviceId, videoId, operationType, operationResult, startTime, endTime, keyword);

            int offset = pageRequest.getOffset();
            int pageSize = pageRequest.getPageSize();

            List<VideoLog> pageRecords = records.stream()
                    .skip(offset)
                    .limit(pageSize)
                    .toList();

            PageResponse<VideoLog> pageResponse = PageResponse.of(pageRecords, total, pageRequest.getPageNum(), pageSize);
            return ResponseUtil.success(pageResponse);
        } catch (Exception e) {
            return ResponseUtil.fail("查询失败: " + e.getMessage());
        }
    }

    @Operation(summary = "写入视频日志")
    @PostMapping("/video/add")
    public SResult<String> videoAdd(@Parameter(description = "视频日志") @RequestBody VideoLog videoLog) {
        try {
            videoLogRepository.save(videoLog);
            return ResponseUtil.success("添加成功");
        } catch (Exception e) {
            return ResponseUtil.fail("添加失败: " + e.getMessage());
        }
    }
}
