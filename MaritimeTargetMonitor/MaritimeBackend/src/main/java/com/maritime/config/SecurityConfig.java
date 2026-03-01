package com.maritime.config;

import com.maritime.security.JwtAuthenticationFilter;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.WebSecurityConfigurerAdapter;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

@Configuration
public class SecurityConfig extends WebSecurityConfigurerAdapter {

    @Autowired
    private JwtAuthenticationFilter jwtAuthenticationFilter;

    @Override
    protected void configure(HttpSecurity http) throws Exception {
        http
            // 禁用CSRF
            .csrf().disable()
            // 禁用会话管理
            .sessionManagement().sessionCreationPolicy(SessionCreationPolicy.STATELESS)
            .and()
            // 配置请求授权
            .authorizeRequests()
            // 允许匿名访问的路径
            .antMatchers("/app/user/login", "/app/user/health").permitAll()
            // 允许Swagger访问
            .antMatchers("/v3/api-docs/**", "/swagger-ui.html", "/swagger-ui/**").permitAll()
            // 允许H2控制台访问
            .antMatchers("/h2-console/**").permitAll()
            // 其他路径需要认证
            .anyRequest().authenticated();

        // 添加JWT认证过滤器
        http.addFilterBefore(jwtAuthenticationFilter, UsernamePasswordAuthenticationFilter.class);

        // 允许H2控制台的iframe
        http.headers().frameOptions().disable();
    }

}
