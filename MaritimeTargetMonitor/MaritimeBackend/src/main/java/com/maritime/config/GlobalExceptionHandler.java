package com.maritime.config;

import com.maritime.dto.SResult;
import com.maritime.utils.ResponseUtil;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

@RestControllerAdvice
public class GlobalExceptionHandler {

    private static final Logger logger = LoggerFactory.getLogger(GlobalExceptionHandler.class);

    /**
     * 处理所有未捕获的异常
     * @param e 异常对象
     * @return 统一错误响应
     */
    @ExceptionHandler(Exception.class)
    public SResult<?> handleException(Exception e) {
        logger.error("系统异常", e);
        return ResponseUtil.internalError();
    }

    /**
     * 处理运行时异常
     * @param e 运行时异常对象
     * @return 统一错误响应
     */
    @ExceptionHandler(RuntimeException.class)
    public SResult<?> handleRuntimeException(RuntimeException e) {
        logger.error("运行时异常", e);
        return ResponseUtil.fail(e.getMessage());
    }

    /**
     * 处理空指针异常
     * @param e 空指针异常对象
     * @return 统一错误响应
     */
    @ExceptionHandler(NullPointerException.class)
    public SResult<?> handleNullPointerException(NullPointerException e) {
        logger.error("空指针异常", e);
        return ResponseUtil.fail("系统内部错误：空指针异常");
    }

    /**
     * 处理参数异常
     * @param e 参数异常对象
     * @return 统一错误响应
     */
    @ExceptionHandler(IllegalArgumentException.class)
    public SResult<?> handleIllegalArgumentException(IllegalArgumentException e) {
        logger.error("参数异常", e);
        return ResponseUtil.fail(e.getMessage());
    }

}
