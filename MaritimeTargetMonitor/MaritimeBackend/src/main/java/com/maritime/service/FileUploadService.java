package com.maritime.service;

import com.maritime.model.FileMetadata;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;

public interface FileUploadService {

    /**
     * 上传文件
     * @param type 文件类型：battery/environment/speed/photo/trajectory
     * @param file 上传的文件
     * @param deviceId 关联的设备ID
     * @param uploader 上传者
     * @return 文件元数据
     * @throws IOException 文件处理异常
     */
    FileMetadata uploadFile(String type, MultipartFile file, String deviceId, String uploader) throws IOException;

    /**
     * 检查上传目录是否可写
     * @return 是否可写
     */
    boolean checkUploadDirWritable();

    /**
     * 清理过期文件
     * @param days 保留天数
     * @return 清理的文件数量
     */
    int cleanExpiredFiles(int days);

    /**
     * 删除文件
     * @param id 文件ID
     * @return 是否删除成功
     */
    boolean deleteFile(Long id);
}
