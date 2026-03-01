package com.maritime.service;

import com.maritime.model.User;
import com.maritime.dto.LoginRequest;
import com.maritime.dto.LoginResponse;

public interface UserService {

    /**
     * 用户登录
     * @param request 登录请求
     * @return 登录响应
     */
    LoginResponse login(LoginRequest request);

    /**
     * 根据用户名获取用户信息
     * @param username 用户名
     * @return 用户信息
     */
    User getUserByUsername(String username);

    /**
     * 验证用户密码
     * @param username 用户名
     * @param password MD5加密的密码
     * @return 是否验证通过
     */
    boolean verifyPassword(String username, String password);

}
