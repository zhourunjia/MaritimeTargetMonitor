package com.maritime.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import com.google.common.util.concurrent.RateLimiter;

@Configuration
public class RateLimitConfig {

    /**
     * 登录接口限流，每秒最多3次请求
     * @return 限流器
     */
    @Bean("loginRateLimiter")
    public RateLimiter loginRateLimiter() {
        return RateLimiter.create(3.0);
    }

}
