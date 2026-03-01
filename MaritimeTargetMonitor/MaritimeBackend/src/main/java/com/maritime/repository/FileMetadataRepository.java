package com.maritime.repository;

import com.maritime.model.FileMetadata;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface FileMetadataRepository extends JpaRepository<FileMetadata, Long> {

    // 根据文件类型查询
    List<FileMetadata> findByTypeOrderByUploadTimeDesc(String type);

    // 根据设备ID查询
    List<FileMetadata> findByDeviceIdOrderByUploadTimeDesc(String deviceId);

    // 根据文件类型和设备ID查询
    List<FileMetadata> findByTypeAndDeviceIdOrderByUploadTimeDesc(String type, String deviceId);

    // 根据文件名查询
    List<FileMetadata> findByFilenameContainingOrderByUploadTimeDesc(String filename);

    // 根据MD5值查询
    FileMetadata findByMd5(String md5);
}
