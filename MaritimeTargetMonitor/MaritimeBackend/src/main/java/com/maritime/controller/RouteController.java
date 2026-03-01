package com.maritime.controller;

import com.maritime.dto.PageRequest;
import com.maritime.dto.PageResponse;
import com.maritime.model.Route;
import com.maritime.repository.RouteRepository;
import com.maritime.utils.SResult;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.Page;

import org.springframework.data.domain.Sort;
import org.springframework.web.bind.annotation.*;

import javax.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.io.PrintWriter;
import java.time.LocalDateTime;

@Tag(name = "轨迹管理")
@RestController
@RequestMapping("/app/route")
public class RouteController {

    @Autowired
    private RouteRepository routeRepository;

    @Operation(summary = "分页查询轨迹列表")
    @PostMapping("/list")
    public SResult<PageResponse<Route>> list(
            @Parameter(description = "分页请求") @RequestBody PageRequest pageRequest,
            @Parameter(description = "轨迹名称") @RequestParam(required = false) String routeName,
            @Parameter(description = "设备ID") @RequestParam(required = false) String deviceId,
            @Parameter(description = "创建人") @RequestParam(required = false) String creator) {
        try {
            Sort sort = Sort.by(Sort.Direction.DESC, "createdAt");
            if (pageRequest.getOrderBy() != null) {
                sort = Sort.by(pageRequest.getOrderDirection().equals("asc") ? Sort.Direction.ASC : Sort.Direction.DESC, pageRequest.getOrderBy());
            }

            Page<Route> page = routeRepository.findAll(
                    (root, query, cb) -> {
                        javax.persistence.criteria.Predicate predicate = cb.conjunction();
                        if (routeName != null && !routeName.isEmpty()) {
                            predicate.getExpressions().add(cb.like(root.get("routeName"), "%" + routeName + "%"));
                        }
                        if (deviceId != null && !deviceId.isEmpty()) {
                            predicate.getExpressions().add(cb.equal(root.get("deviceId"), deviceId));
                        }
                        if (creator != null && !creator.isEmpty()) {
                            predicate.getExpressions().add(cb.like(root.get("creator"), "%" + creator + "%"));
                        }
                        return predicate;
                    },
                    org.springframework.data.domain.PageRequest.of(pageRequest.getPageNum() - 1, pageRequest.getPageSize(), sort)
            );

            return SResult.success(
                    PageResponse.of(
                            page.getContent(),
                            page.getTotalElements(),
                            pageRequest.getPageNum(),
                            pageRequest.getPageSize()
                    )
            );
        } catch (Exception e) {
            return SResult.error(500, "查询轨迹列表失败: " + e.getMessage());
        }
    }

    @Operation(summary = "根据ID查询轨迹详情")
    @GetMapping("/detail")
    public SResult<Route> detail(@Parameter(description = "轨迹ID") @RequestParam Long id) {
        try {
            return routeRepository.findById(id)
                    .map(SResult::success)
                    .orElse(SResult.error(404, "轨迹不存在"));
        } catch (Exception e) {
            return SResult.error(500, "查询轨迹详情失败: " + e.getMessage());
        }
    }

    @Operation(summary = "新增轨迹")
    @PostMapping("/add")
    public SResult<Route> add(@Parameter(description = "轨迹信息") @RequestBody Route route) {
        try {
            route.setCreatedAt(LocalDateTime.now());
            route.setUpdatedAt(LocalDateTime.now());
            Route saved = routeRepository.save(route);
            return SResult.success(saved);
        } catch (Exception e) {
            return SResult.error(500, "新增轨迹失败: " + e.getMessage());
        }
    }

    @Operation(summary = "更新轨迹")
    @PostMapping("/update")
    public SResult<Route> update(@Parameter(description = "轨迹信息") @RequestBody Route route) {
        try {
            if (!routeRepository.existsById(route.getId())) {
                return SResult.error(404, "轨迹不存在");
            }
            route.setUpdatedAt(LocalDateTime.now());
            Route updated = routeRepository.save(route);
            return SResult.success(updated);
        } catch (Exception e) {
            return SResult.error(500, "更新轨迹失败: " + e.getMessage());
        }
    }

    @Operation(summary = "删除轨迹")
    @PostMapping("/delete")
    public SResult<Void> delete(@Parameter(description = "轨迹ID") @RequestParam Long id) {
        try {
            if (!routeRepository.existsById(id)) {
                return SResult.error(404, "轨迹不存在");
            }
            routeRepository.deleteById(id);
            return SResult.success();
        } catch (Exception e) {
            return SResult.error(500, "删除轨迹失败: " + e.getMessage());
        }
    }

    @Operation(summary = "导出轨迹")
    @GetMapping("/export")
    public void export(
            HttpServletResponse response,
            @Parameter(description = "轨迹ID") @RequestParam Long id) throws IOException {
        response.setContentType("application/json;charset=UTF-8");
        response.setHeader("Content-Disposition", "attachment;filename=route_" + id + ".json");

        try {
            Route route = routeRepository.findById(id)
                    .orElseThrow(() -> new IllegalArgumentException("轨迹不存在"));

            PrintWriter writer = response.getWriter();
            writer.write("{");
            writer.write("\"id\":\"" + route.getId() + "\",");
            writer.write("\"routeName\":\"" + route.getRouteName() + "\",");
            writer.write("\"deviceId\":\"" + route.getDeviceId() + "\",");
            writer.write("\"points\":" + route.getPoints() + ",");
            writer.write("\"description\":\"" + route.getDescription() + "\",");
            writer.write("\"creator\":\"" + route.getCreator() + "\",");
            writer.write("\"createdAt\":\"" + route.getCreatedAt() + "\",");
            writer.write("\"updatedAt\":\"" + route.getUpdatedAt() + "\"");
            writer.write("}");
            writer.flush();
        } catch (Exception e) {
            response.setStatus(500);
            PrintWriter writer = response.getWriter();
            writer.write("{\"error\":\"导出轨迹失败: " + e.getMessage() + "\"}");
            writer.flush();
        }
    }

    @Operation(summary = "批量导出轨迹")
    @PostMapping("/export/batch")
    public void batchExport(
            HttpServletResponse response,
            @Parameter(description = "轨迹ID列表") @RequestParam Long[] ids) throws IOException {
        response.setContentType("application/json;charset=UTF-8");
        response.setHeader("Content-Disposition", "attachment;filename=routes_batch_" + System.currentTimeMillis() + ".json");

        try {
            java.util.List<Route> routes = routeRepository.findAllById(java.util.Arrays.asList(ids));

            PrintWriter writer = response.getWriter();
            writer.write("[");
            boolean first = true;
            for (Route route : routes) {
                if (!first) writer.write(",");
                writer.write("{");
                writer.write("\"id\":\"" + route.getId() + "\",");
                writer.write("\"routeName\":\"" + route.getRouteName() + "\",");
                writer.write("\"deviceId\":\"" + route.getDeviceId() + "\",");
                writer.write("\"points\":" + route.getPoints() + ",");
                writer.write("\"description\":\"" + route.getDescription() + "\",");
                writer.write("\"creator\":\"" + route.getCreator() + "\",");
                writer.write("\"createdAt\":\"" + route.getCreatedAt() + "\",");
                writer.write("\"updatedAt\":\"" + route.getUpdatedAt() + "\"");
                writer.write("}");
                first = false;
            }
            writer.write("]");
            writer.flush();
        } catch (Exception e) {
            response.setStatus(500);
            PrintWriter writer = response.getWriter();
            writer.write("{\"error\":\"批量导出轨迹失败: " + e.getMessage() + "\"}");
            writer.flush();
        }
    }
}
