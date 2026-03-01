package com.maritime.aspect;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.maritime.model.AuditLog;
import com.maritime.repository.AuditLogRepository;
import com.maritime.utils.JwtUtil;
import org.aspectj.lang.ProceedingJoinPoint;
import org.aspectj.lang.annotation.Around;
import org.aspectj.lang.annotation.Aspect;
import org.aspectj.lang.annotation.Pointcut;
import org.aspectj.lang.reflect.MethodSignature;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

import javax.servlet.http.HttpServletRequest;
import java.lang.reflect.Method;
import java.time.LocalDateTime;

@Aspect
@Component
public class AuditLogAspect {

    private static final Logger logger = LoggerFactory.getLogger(AuditLogAspect.class);

    @Autowired
    private AuditLogRepository auditLogRepository;

    @Autowired
    private ObjectMapper objectMapper;

    @Autowired
    private JwtUtil jwtUtil;

    @Pointcut("@annotation(io.swagger.v3.oas.annotations.Operation)")
    public void apiOperationPointcut() {
    }

    @Around("apiOperationPointcut()")
    public Object around(ProceedingJoinPoint joinPoint) throws Throwable {
        long startTime = System.currentTimeMillis();
        AuditLog auditLog = new AuditLog();

        try {
            ServletRequestAttributes attributes = (ServletRequestAttributes) RequestContextHolder.getRequestAttributes();
            if (attributes != null) {
                HttpServletRequest request = attributes.getRequest();
                auditLog.setIpAddress(getClientIpAddress(request));
            }

            MethodSignature signature = (MethodSignature) joinPoint.getSignature();
            Method method = signature.getMethod();
            io.swagger.v3.oas.annotations.Operation operation = method.getAnnotation(io.swagger.v3.oas.annotations.Operation.class);

            if (operation != null) {
                auditLog.setOperationType(operation.summary());
            }

            String className = joinPoint.getTarget().getClass().getSimpleName();
            String methodName = method.getName();
            auditLog.setModule(className);
            auditLog.setMethod(methodName);

            Object[] args = joinPoint.getArgs();
            if (args != null && args.length > 0) {
                try {
                    String paramsJson = objectMapper.writeValueAsString(args);
                    auditLog.setParams(paramsJson);
                } catch (Exception e) {
                    auditLog.setParams("参数序列化失败");
                }
            }

            String operator = getCurrentUser();
            auditLog.setOperator(operator);

            Object result = joinPoint.proceed();

            auditLog.setResult("success");
            auditLog.setResultMessage("操作成功");

            long duration = System.currentTimeMillis() - startTime;
            auditLog.setDuration(duration);

            auditLogRepository.save(auditLog);

            return result;
        } catch (Exception e) {
            auditLog.setResult("failed");
            auditLog.setResultMessage(e.getMessage());

            long duration = System.currentTimeMillis() - startTime;
            auditLog.setDuration(duration);

            try {
                auditLogRepository.save(auditLog);
            } catch (Exception saveException) {
                logger.error("保存审计日志失败", saveException);
            }

            throw e;
        }
    }

    private String getCurrentUser() {
        try {
            ServletRequestAttributes attributes = (ServletRequestAttributes) RequestContextHolder.getRequestAttributes();
            if (attributes != null) {
                HttpServletRequest request = attributes.getRequest();
                String token = request.getHeader("Authorization");
                if (token != null && token.startsWith("Bearer ")) {
                    token = token.substring(7);
                    return jwtUtil.getUsernameFromToken(token);
                }
            }
        } catch (Exception e) {
            logger.error("获取当前用户失败", e);
        }
        return "anonymous";
    }

    private String getClientIpAddress(HttpServletRequest request) {
        String ip = request.getHeader("X-Forwarded-For");
        if (ip == null || ip.isEmpty() || "unknown".equalsIgnoreCase(ip)) {
            ip = request.getHeader("X-Real-IP");
        }
        if (ip == null || ip.isEmpty() || "unknown".equalsIgnoreCase(ip)) {
            ip = request.getHeader("Proxy-Client-IP");
        }
        if (ip == null || ip.isEmpty() || "unknown".equalsIgnoreCase(ip)) {
            ip = request.getHeader("WL-Proxy-Client-IP");
        }
        if (ip == null || ip.isEmpty() || "unknown".equalsIgnoreCase(ip)) {
            ip = request.getRemoteAddr();
        }
        if (ip != null && ip.contains(",")) {
            ip = ip.split(",")[0].trim();
        }
        return ip;
    }
}