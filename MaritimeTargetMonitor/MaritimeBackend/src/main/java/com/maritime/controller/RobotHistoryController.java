package com.maritime.controller;

import com.maritime.dto.PageRequest;
import com.maritime.dto.PageResponse;
import com.maritime.dto.SResult;
import com.maritime.model.RobotHistory;
import com.maritime.repository.RobotHistoryRepository;
import com.maritime.utils.ResponseUtil;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDateTime;
import java.util.List;

@Tag(name = "机器人历史记录")
@RestController
@RequestMapping("/app/robot-history")
public class RobotHistoryController {

    @Autowired
    private RobotHistoryRepository robotHistoryRepository;

    @Operation(summary = "分页查询机器人历史记录")
    @PostMapping("/list")
    public SResult<PageResponse<RobotHistory>> list(
            @Parameter(description = "分页请求") @RequestBody PageRequest pageRequest,
            @Parameter(description = "设备ID") @RequestParam(required = false) String deviceId,
            @Parameter(description = "任务ID") @RequestParam(required = false) String taskId,
            @Parameter(description = "任务类型") @RequestParam(required = false) String taskType,
            @Parameter(description = "运行状态") @RequestParam(required = false) String runStatus,
            @Parameter(description = "开始时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime startTime,
            @Parameter(description = "结束时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime endTime,
            @Parameter(description = "关键字") @RequestParam(required = false) String keyword) {

        try {
            List<RobotHistory> records = robotHistoryRepository.findByConditions(deviceId, taskId, taskType, runStatus, startTime, endTime, keyword);
            Long total = robotHistoryRepository.countByConditions(deviceId, taskId, taskType, runStatus, startTime, endTime, keyword);

            int offset = pageRequest.getOffset();
            int pageSize = pageRequest.getPageSize();

            List<RobotHistory> pageRecords = records.stream()
                    .skip(offset)
                    .limit(pageSize)
                    .toList();

            PageResponse<RobotHistory> pageResponse = PageResponse.of(pageRecords, total, pageRequest.getPageNum(), pageSize);
            return ResponseUtil.success(pageResponse);
        } catch (Exception e) {
            return ResponseUtil.fail("查询失败: " + e.getMessage());
        }
    }

    @Operation(summary = "获取机器人历史详情")
    @GetMapping("/detail/{id}")
    public SResult<RobotHistory> detail(@Parameter(description = "历史记录ID") @PathVariable Long id) {
        try {
            RobotHistory record = robotHistoryRepository.findById(id)
                    .orElseThrow(() -> new RuntimeException("历史记录不存在"));

            return ResponseUtil.success(record);
        } catch (Exception e) {
            return ResponseUtil.fail("查询失败: " + e.getMessage());
        }
    }

    @Operation(summary = "按设备查询历史记录")
    @GetMapping("/device/{deviceId}")
    public SResult<List<RobotHistory>> getByDevice(@Parameter(description = "设备ID") @PathVariable String deviceId) {
        try {
            List<RobotHistory> records = robotHistoryRepository.findByDeviceIdOrderByStartTimeDesc(deviceId);
            return ResponseUtil.success(records);
        } catch (Exception e) {
            return ResponseUtil.fail("查询失败: " + e.getMessage());
        }
    }

    @Operation(summary = "按任务查询历史记录")
    @GetMapping("/task/{taskId}")
    public SResult<List<RobotHistory>> getByTask(@Parameter(description = "任务ID") @PathVariable String taskId) {
        try {
            List<RobotHistory> records = robotHistoryRepository.findByTaskIdOrderByStartTimeDesc(taskId);
            return ResponseUtil.success(records);
        } catch (Exception e) {
            return ResponseUtil.fail("查询失败: " + e.getMessage());
        }
    }

    @Operation(summary = "添加机器人历史记录")
    @PostMapping("/add")
    public SResult<String> add(@Parameter(description = "机器人历史记录") @RequestBody RobotHistory robotHistory) {
        try {
            robotHistoryRepository.save(robotHistory);
            return ResponseUtil.success("添加成功");
        } catch (Exception e) {
            return ResponseUtil.fail("添加失败: " + e.getMessage());
        }
    }

    @Operation(summary = "更新机器人历史记录")
    @PutMapping("/update/{id}")
    public SResult<String> update(
            @Parameter(description = "历史记录ID") @PathVariable Long id,
            @Parameter(description = "机器人历史记录") @RequestBody RobotHistory robotHistory) {

        try {
            RobotHistory existing = robotHistoryRepository.findById(id)
                    .orElseThrow(() -> new RuntimeException("历史记录不存在"));

            existing.setDeviceId(robotHistory.getDeviceId());
            existing.setDeviceName(robotHistory.getDeviceName());
            existing.setTaskId(robotHistory.getTaskId());
            existing.setTaskName(robotHistory.getTaskName());
            existing.setTaskType(robotHistory.getTaskType());
            existing.setStartTime(robotHistory.getStartTime());
            existing.setEndTime(robotHistory.getEndTime());
            existing.setDuration(robotHistory.getDuration());
            existing.setDistance(robotHistory.getDistance());
            existing.setRunStatus(robotHistory.getRunStatus());
            existing.setStartX(robotHistory.getStartX());
            existing.setStartY(robotHistory.getStartY());
            existing.setEndX(robotHistory.getEndX());
            existing.setEndY(robotHistory.getEndY());
            existing.setTrajectoryData(robotHistory.getTrajectoryData());
            existing.setRemark(robotHistory.getRemark());

            robotHistoryRepository.save(existing);
            return ResponseUtil.success("更新成功");
        } catch (Exception e) {
            return ResponseUtil.fail("更新失败: " + e.getMessage());
        }
    }

    @Operation(summary = "删除机器人历史记录")
    @DeleteMapping("/delete/{id}")
    public SResult<String> delete(@Parameter(description = "历史记录ID") @PathVariable Long id) {
        try {
            robotHistoryRepository.deleteById(id);
            return ResponseUtil.success("删除成功");
        } catch (Exception e) {
            return ResponseUtil.fail("删除失败: " + e.getMessage());
        }
    }
}
