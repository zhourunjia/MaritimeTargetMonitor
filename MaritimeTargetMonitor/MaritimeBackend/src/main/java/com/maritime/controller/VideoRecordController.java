package com.maritime.controller;

import com.maritime.dto.PageRequest;
import com.maritime.dto.PageResponse;
import com.maritime.dto.SResult;
import com.maritime.model.VideoRecord;
import com.maritime.repository.VideoRecordRepository;
import com.maritime.utils.ResponseUtil;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.web.bind.annotation.*;

import java.io.File;
import java.nio.file.Files;
import java.time.LocalDateTime;
import java.util.List;

@Tag(name = "视频记录管理")
@RestController
@RequestMapping("/app/video")
public class VideoRecordController {

    @Autowired
    private VideoRecordRepository videoRecordRepository;

    @Operation(summary = "分页查询视频记录")
    @PostMapping("/list")
    public SResult<PageResponse<VideoRecord>> list(
            @Parameter(description = "分页请求") @RequestBody PageRequest pageRequest,
            @Parameter(description = "设备ID") @RequestParam(required = false) String deviceId,
            @Parameter(description = "视频名称") @RequestParam(required = false) String videoName,
            @Parameter(description = "开始时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime startTime,
            @Parameter(description = "结束时间") @RequestParam(required = false) @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss") LocalDateTime endTime) {

        try {
            List<VideoRecord> records = videoRecordRepository.findByConditions(deviceId, videoName, startTime, endTime);
            Long total = videoRecordRepository.countByConditions(deviceId, videoName, startTime, endTime);

            int offset = pageRequest.getOffset();
            int pageSize = pageRequest.getPageSize();

            List<VideoRecord> pageRecords = records.stream()
                    .skip(offset)
                    .limit(pageSize)
                    .toList();

            PageResponse<VideoRecord> pageResponse = PageResponse.of(pageRecords, total, pageRequest.getPageNum(), pageSize);
            return ResponseUtil.success(pageResponse);
        } catch (Exception e) {
            return ResponseUtil.fail("查询失败: " + e.getMessage());
        }
    }

    @Operation(summary = "下载视频")
    @GetMapping("/download/{id}")
    public SResult<String> download(@Parameter(description = "视频ID") @PathVariable Long id) {
        try {
            VideoRecord record = videoRecordRepository.findById(id)
                    .orElseThrow(() -> new RuntimeException("视频记录不存在"));

            String filePath = record.getFilePath();
            File file = new File(filePath);

            if (!file.exists()) {
                return ResponseUtil.fail("视频文件不存在");
            }

            byte[] fileContent = Files.readAllBytes(file.toPath());

            return ResponseUtil.success("下载成功，文件大小: " + fileContent.length + " 字节");
        } catch (Exception e) {
            return ResponseUtil.fail("下载失败: " + e.getMessage());
        }
    }

    @Operation(summary = "删除视频记录")
    @DeleteMapping("/delete/{id}")
    public SResult<String> delete(@Parameter(description = "视频ID") @PathVariable Long id) {
        try {
            VideoRecord record = videoRecordRepository.findById(id)
                    .orElseThrow(() -> new RuntimeException("视频记录不存在"));

            String filePath = record.getFilePath();
            File file = new File(filePath);

            if (file.exists()) {
                file.delete();
            }

            videoRecordRepository.deleteById(id);

            return ResponseUtil.success("删除成功");
        } catch (Exception e) {
            return ResponseUtil.fail("删除失败: " + e.getMessage());
        }
    }

    @Operation(summary = "批量删除视频记录")
    @DeleteMapping("/batch-delete")
    public SResult<String> batchDelete(@Parameter(description = "视频ID列表") @RequestBody List<Long> ids) {
        try {
            int successCount = 0;
            int failCount = 0;

            for (Long id : ids) {
                try {
                    VideoRecord record = videoRecordRepository.findById(id)
                            .orElseThrow(() -> new RuntimeException("视频记录不存在"));

                    String filePath = record.getFilePath();
                    File file = new File(filePath);

                    if (file.exists()) {
                        file.delete();
                    }

                    videoRecordRepository.deleteById(id);
                    successCount++;
                } catch (Exception e) {
                    failCount++;
                }
            }

            return ResponseUtil.success(String.format("批量删除完成，成功: %d, 失败: %d", successCount, failCount));
        } catch (Exception e) {
            return ResponseUtil.fail("批量删除失败: " + e.getMessage());
        }
    }

    @Operation(summary = "获取视频详情")
    @GetMapping("/detail/{id}")
    public SResult<VideoRecord> detail(@Parameter(description = "视频ID") @PathVariable Long id) {
        try {
            VideoRecord record = videoRecordRepository.findById(id)
                    .orElseThrow(() -> new RuntimeException("视频记录不存在"));

            return ResponseUtil.success(record);
        } catch (Exception e) {
            return ResponseUtil.fail("查询失败: " + e.getMessage());
        }
    }
}
