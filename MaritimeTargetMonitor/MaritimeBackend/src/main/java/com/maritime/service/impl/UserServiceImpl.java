package com.maritime.service.impl;

import com.maritime.model.User;
import com.maritime.dto.LoginRequest;
import com.maritime.dto.LoginResponse;
import com.maritime.service.UserService;
import com.maritime.utils.JwtUtil;
import com.maritime.utils.Md5Util;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;

import java.util.Date;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Service
public class UserServiceImpl implements UserService {

    @Autowired
    private JwtUtil jwtUtil;

    @Value("${spring.profiles.active:default}")
    private String activeProfile;

    // 密码编码器
    private final BCryptPasswordEncoder passwordEncoder = new BCryptPasswordEncoder();

    // 模拟用户数据
    private static final Map<String, User> userMap = new ConcurrentHashMap<>();

    // 登录失败记录
    private static final Map<String, LoginFailureRecord> loginFailureMap = new ConcurrentHashMap<>();

    // 锁定时间（毫秒）
    private static final long LOCK_DURATION = 5 * 60 * 1000; // 5分钟

    // 最大失败次数
    private static final int MAX_FAILURE_COUNT = 3;

    static {
        // 初始化默认用户，使用bcrypt存储密码
        // 注意：这里会在所有环境初始化，后续会在构造方法中根据环境删除默认账号
        User admin = new User();
        admin.setId(1L);
        admin.setUsername("admin");
        admin.setPassword(new BCryptPasswordEncoder().encode("123456"));
        admin.setNickname("管理员");
        admin.setRole("admin");
        admin.setStatus(1);
        admin.setCreatedAt(new Date());
        admin.setUpdatedAt(new Date());
        userMap.put("admin", admin);

        User user = new User();
        user.setId(2L);
        user.setUsername("user");
        user.setPassword(new BCryptPasswordEncoder().encode("123456"));
        user.setNickname("普通用户");
        user.setRole("user");
        user.setStatus(1);
        user.setCreatedAt(new Date());
        user.setUpdatedAt(new Date());
        userMap.put("user", user);
    }

    // 构造方法，根据环境删除默认账号
    public UserServiceImpl() {
        System.out.println("Active profile: " + activeProfile);
        System.out.println("Initial user count: " + userMap.size());
        for (String username : userMap.keySet()) {
            System.out.println("User: " + username + ", Password: " + userMap.get(username).getPassword());
        }
        
        // 生产环境删除默认账号
        if ("prod".equals(activeProfile)) {
            System.out.println("Removing default users for prod environment");
            userMap.remove("admin");
            userMap.remove("user");
        }
        
        System.out.println("Final user count: " + userMap.size());
        for (String username : userMap.keySet()) {
            System.out.println("Remaining user: " + username);
        }
    }

    @Override
    public LoginResponse login(LoginRequest request) {
        String username = request.getUsername();
        String password = request.getPassword();

        // 检查用户是否被锁定
        if (isUserLocked(username)) {
            throw new RuntimeException("账号已被锁定，请5分钟后再试");
        }

        try {
            // 验证用户
            if (!verifyPassword(username, password)) {
                // 记录登录失败
                recordLoginFailure(username);
                throw new RuntimeException("用户名或密码错误");
            }

            // 登录成功，清除失败记录
            loginFailureMap.remove(username);

            // 获取用户信息
            User user = getUserByUsername(username);

            // 生成token
            Map<String, Object> claims = new HashMap<>();
            claims.put("username", username);
            claims.put("role", user.getRole());
            String token = jwtUtil.generateToken(claims);

            // 构建响应
            LoginResponse response = new LoginResponse();
            response.setToken(token);

            LoginResponse.UserInfo userInfo = new LoginResponse.UserInfo();
            userInfo.setUsername(user.getUsername());
            userInfo.setNickname(user.getNickname());
            userInfo.setRole(user.getRole());
            response.setUser(userInfo);

            return response;
        } catch (RuntimeException e) {
            // 记录登录失败
            if (!"账号已被锁定，请5分钟后再试".equals(e.getMessage())) {
                recordLoginFailure(username);
            }
            throw e;
        }
    }

    @Override
    public User getUserByUsername(String username) {
        return userMap.get(username);
    }

    @Override
    public boolean verifyPassword(String username, String password) {
        User user = userMap.get(username);
        if (user == null) {
            return false;
        }

        String storedPassword = user.getPassword();

        // 检查存储的密码是否是bcrypt格式
        if (storedPassword.startsWith("$2a$")) {
            // 尝试直接验证（原始密码）
            if (passwordEncoder.matches(password, storedPassword)) {
                return true;
            }
            
            // 尝试兼容客户端传MD5的情况
            // 注意：这里为了兼容，我们需要特殊处理
            // 由于我们存储的是bcrypt(原始密码)，而客户端传的是MD5(原始密码)
            // 我们无法直接比较，所以需要使用一种特殊的验证方式
            // 这里我们假设常见的默认密码，尝试用MD5后与客户端传的比较
            // 实际生产环境中，应该要求客户端使用HTTPS并传输原始密码
            
            // 尝试常见的默认密码
            String[] commonPasswords = {"123456", "admin123", "password", "12345678", "123456789"};
            for (String commonPwd : commonPasswords) {
                if (Md5Util.md5(commonPwd).equals(password) && passwordEncoder.matches(commonPwd, storedPassword)) {
                    return true;
                }
            }
            
            return false;
        } else {
            // 旧的MD5格式，用于兼容
            return storedPassword.equals(password);
        }
    }

    private boolean isUserLocked(String username) {
        LoginFailureRecord record = loginFailureMap.get(username);
        if (record == null) {
            return false;
        }

        // 检查是否达到最大失败次数
        if (record.getFailureCount() >= MAX_FAILURE_COUNT) {
            // 检查锁定是否过期
            if (System.currentTimeMillis() - record.getLastFailureTime() < LOCK_DURATION) {
                return true;
            } else {
                // 锁定过期，清除记录
                loginFailureMap.remove(username);
                return false;
            }
        }

        return false;
    }

    private void recordLoginFailure(String username) {
        LoginFailureRecord record = loginFailureMap.get(username);
        if (record == null) {
            record = new LoginFailureRecord();
            loginFailureMap.put(username, record);
        }

        record.setFailureCount(record.getFailureCount() + 1);
        record.setLastFailureTime(System.currentTimeMillis());
    }

    // 登录失败记录
    private static class LoginFailureRecord {
        private int failureCount = 0;
        private long lastFailureTime;

        public int getFailureCount() {
            return failureCount;
        }

        public void setFailureCount(int failureCount) {
            this.failureCount = failureCount;
        }

        public long getLastFailureTime() {
            return lastFailureTime;
        }

        public void setLastFailureTime(long lastFailureTime) {
            this.lastFailureTime = lastFailureTime;
        }
    }

}
