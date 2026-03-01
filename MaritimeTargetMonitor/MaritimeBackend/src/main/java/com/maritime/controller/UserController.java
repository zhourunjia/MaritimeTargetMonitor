package com.maritime.controller;

import com.google.common.util.concurrent.RateLimiter;
import com.maritime.dto.LoginRequest;
import com.maritime.dto.SResult;
import com.maritime.service.UserService;
import com.maritime.utils.ResponseUtil;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/app/user")
public class UserController {

    @Autowired
    private UserService userService;

    @Autowired
    @Qualifier("loginRateLimiter")
    private RateLimiter loginRateLimiter;

    /**
     * 用户登录
     * @param request 登录请求
     * @return 登录响应
     */
    @PostMapping("/login")
    public SResult<?> login(@RequestBody LoginRequest request) {
        // 登录限流
        if (!loginRateLimiter.tryAcquire()) {
            return ResponseUtil.fail("登录请求过于频繁，请稍后再试");
        }

        try {
            return ResponseUtil.success(userService.login(request));
        } catch (Exception e) {
            return ResponseUtil.fail(e.getMessage());
        }
    }

    /**
     * 健康检查
     * @return 健康状态
     */
    @GetMapping("/health")
    public SResult<?> health() {
        // 从SecurityContextHolder中获取当前认证的用户
        org.springframework.security.core.Authentication authentication = 
            org.springframework.security.core.context.SecurityContextHolder.getContext().getAuthentication();
        
        if (authentication != null && authentication.isAuthenticated() && !"anonymousUser".equals(authentication.getPrincipal())) {
            // 用户已认证，返回用户态健康信息
            String username = (String) authentication.getPrincipal();
            return ResponseUtil.success("ok, user: " + username);
        } else {
            // 用户未认证，返回基本健康状态
            return ResponseUtil.success("ok");
        }
    }

}
