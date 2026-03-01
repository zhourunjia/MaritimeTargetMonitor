package com.maritime.controller;

import com.maritime.dto.SResult;
import com.maritime.service.PermissionService;
import com.maritime.utils.ResponseUtil;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/app/permission")
public class PermissionController {

    @Autowired
    private PermissionService permissionService;

    /**
     * 获取用户的菜单权限
     * @return 菜单树
     */
    @GetMapping("/list/self")
    public SResult<?> getMenuList() {
        try {
            // 从SecurityContextHolder中获取当前认证的用户
            org.springframework.security.core.Authentication authentication = 
                org.springframework.security.core.context.SecurityContextHolder.getContext().getAuthentication();
            
            System.out.println("Authentication: " + authentication);
            if (authentication != null) {
                System.out.println("Authenticated: " + authentication.isAuthenticated());
                System.out.println("Principal: " + authentication.getPrincipal());
            }
            
            if (authentication != null && authentication.isAuthenticated() && !"anonymousUser".equals(authentication.getPrincipal())) {
                String username = (String) authentication.getPrincipal();
                System.out.println("Username: " + username);
                return ResponseUtil.success(permissionService.getMenuListByUser(username));
            } else {
                return ResponseUtil.fail("用户未认证");
            }
        } catch (Exception e) {
            System.out.println("Exception: " + e.getMessage());
            e.printStackTrace();
            return ResponseUtil.fail(e.getMessage());
        }
    }

}
