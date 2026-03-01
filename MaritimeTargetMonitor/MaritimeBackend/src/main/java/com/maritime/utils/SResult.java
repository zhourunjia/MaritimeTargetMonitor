package com.maritime.utils;

import io.swagger.v3.oas.annotations.media.Schema;

@Schema(description = "统一响应结果")
public class SResult<T> {

    @Schema(description = "状态码：0成功，非0失败")
    private int code;

    @Schema(description = "消息")
    private String message;

    @Schema(description = "数据")
    private T data;

    private SResult(int code, String message, T data) {
        this.code = code;
        this.message = message;
        this.data = data;
    }

    public static <T> SResult<T> success() {
        return new SResult<>(0, "success", null);
    }

    public static <T> SResult<T> success(T data) {
        return new SResult<>(0, "success", data);
    }

    public static <T> SResult<T> error(int code, String message) {
        return new SResult<>(code, message, null);
    }

    public int getCode() {
        return code;
    }

    public void setCode(int code) {
        this.code = code;
    }

    public String getMessage() {
        return message;
    }

    public void setMessage(String message) {
        this.message = message;
    }

    public T getData() {
        return data;
    }

    public void setData(T data) {
        this.data = data;
    }
}