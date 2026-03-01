package com.maritime.service;

import com.maritime.model.Menu;

import java.util.List;

public interface PermissionService {

    /**
     * 获取用户的菜单权限
     * @param username 用户名
     * @return 菜单树
     */
    List<Menu> getMenuListByUser(String username);

}
