package com.maritime.utils;

import org.apache.commons.codec.digest.DigestUtils;

public class Md5Util {

    /**
     * 生成MD5哈希值
     * @param text 明文
     * @return MD5哈希值
     */
    public static String md5(String text) {
        return DigestUtils.md5Hex(text);
    }

    /**
     * 验证MD5哈希值
     * @param text 明文
     * @param md5 MD5哈希值
     * @return 是否匹配
     */
    public static boolean verify(String text, String md5) {
        return md5(text).equals(md5);
    }

}
