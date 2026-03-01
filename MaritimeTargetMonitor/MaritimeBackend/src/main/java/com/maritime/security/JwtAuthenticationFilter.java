package com.maritime.security;

import com.maritime.utils.JwtUtil;
import io.jsonwebtoken.Claims;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.web.authentication.WebAuthenticationDetailsSource;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import javax.servlet.FilterChain;
import javax.servlet.ServletException;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.util.ArrayList;

@Component
public class JwtAuthenticationFilter extends OncePerRequestFilter {

    @Autowired
    private JwtUtil jwtUtil;

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain) throws ServletException, IOException {
        System.out.println("JwtAuthenticationFilter.doFilterInternal() called");
        
        // 获取Authorization头
        String authorization = request.getHeader("Authorization");

        System.out.println("Authorization header: " + authorization);
        
        // 检查Authorization头格式
        if (authorization != null && authorization.startsWith("Bearer ")) {
            String token = authorization.substring(7);
            System.out.println("Token: " + token);

            try {
                // 解析token
                System.out.println("Parsing token...");
                Claims claims = jwtUtil.parseToken(token);
                System.out.println("Claims: " + claims);
                String username = (String) claims.get("username");
                System.out.println("Username: " + username);

                // 设置认证信息
                if (username != null && SecurityContextHolder.getContext().getAuthentication() == null) {
                    System.out.println("Setting authentication...");
                    UsernamePasswordAuthenticationToken authentication = new UsernamePasswordAuthenticationToken(
                            username, null, new ArrayList<>());
                    authentication.setDetails(new WebAuthenticationDetailsSource().buildDetails(request));
                    SecurityContextHolder.getContext().setAuthentication(authentication);
                    System.out.println("Authentication set: " + authentication);
                }
            } catch (Exception e) {
                // token无效，忽略
                System.out.println("Token parsing error: " + e.getMessage());
                e.printStackTrace();
            }
        } else {
            System.out.println("No valid Authorization header found");
        }

        System.out.println("Before filterChain.doFilter()");
        filterChain.doFilter(request, response);
        System.out.println("After filterChain.doFilter()");
    }

}
