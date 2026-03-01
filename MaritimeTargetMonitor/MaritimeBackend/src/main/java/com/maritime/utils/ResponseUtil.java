package com.maritime.utils;

import com.maritime.dto.SResult;

public class ResponseUtil {

    // 成功状态码
    public static final int SUCCESS_CODE = 10000;
    public static final String SUCCESS_MESSAGE = "成功";

    // 失败状态码
    public static final int FAIL_CODE = 99999;
    public static final String FAIL_MESSAGE = "失败";

    // 未授权状态码
    public static final int UNAUTHORIZED_CODE = 401;
    public static final String UNAUTHORIZED_MESSAGE = "未授权";

    // 禁止访问状态码
    public static final int FORBIDDEN_CODE = 403;
    public static final String FORBIDDEN_MESSAGE = "禁止访问";

    // 资源不存在状态码
    public static final int NOT_FOUND_CODE = 404;
    public static final String NOT_FOUND_MESSAGE = "资源不存在";

    // 服务器内部错误状态码
    public static final int INTERNAL_ERROR_CODE = 500;
    public static final String INTERNAL_ERROR_MESSAGE = "服务器内部错误";

    /**
     * 生成成功响应
     * @param data 响应数据
     * @param <T> 数据类型
     * @return 成功响应
     */
    public static <T> SResult<T> success(T data) {
        return new SResult<>(SUCCESS_CODE, SUCCESS_MESSAGE, data);
    }

    /**
     * 生成成功响应（无数据）
     * @return 成功响应
     */
    public static SResult<Void> success() {
        return new SResult<>(SUCCESS_CODE, SUCCESS_MESSAGE, null);
    }

    /**
     * 生成失败响应
     * @param message 失败消息
     * @param <T> 数据类型
     * @return 失败响应
     */
    public static <T> SResult<T> fail(String message) {
        return new SResult<>(FAIL_CODE, message, null);
    }

    /**
     * 生成失败响应（默认消息）
     * @param <T> 数据类型
     * @return 失败响应
     */
    public static <T> SResult<T> fail() {
        return new SResult<>(FAIL_CODE, FAIL_MESSAGE, null);
    }

    /**
     * 生成未授权响应
     * @param <T> 数据类型
     * @return 未授权响应
     */
    public static <T> SResult<T> unauthorized() {
        return new SResult<>(UNAUTHORIZED_CODE, UNAUTHORIZED_MESSAGE, null);
    }

    /**
     * 生成禁止访问响应
     * @param <T> 数据类型
     * @return 禁止访问响应
     */
    public static <T> SResult<T> forbidden() {
        return new SResult<>(FORBIDDEN_CODE, FORBIDDEN_MESSAGE, null);
    }

    /**
     * 生成资源不存在响应
     * @param <T> 数据类型
     * @return 资源不存在响应
     */
    public static <T> SResult<T> notFound() {
        return new SResult<>(NOT_FOUND_CODE, NOT_FOUND_MESSAGE, null);
    }

    /**
     * 生成服务器内部错误响应
     * @param <T> 数据类型
     * @return 服务器内部错误响应
     */
    public static <T> SResult<T> internalError() {
        return new SResult<>(INTERNAL_ERROR_CODE, INTERNAL_ERROR_MESSAGE, null);
    }

}
