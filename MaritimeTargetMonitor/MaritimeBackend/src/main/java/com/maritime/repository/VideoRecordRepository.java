package com.maritime.repository;

import com.maritime.model.VideoRecord;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;

@Repository
public interface VideoRecordRepository extends JpaRepository<VideoRecord, Long> {

    @Query("SELECT v FROM VideoRecord v WHERE " +
           "(:deviceId IS NULL OR v.deviceId = :deviceId) AND " +
           "(:videoName IS NULL OR v.videoName LIKE %:videoName%) AND " +
           "(:startTime IS NULL OR v.addTime >= :startTime) AND " +
           "(:endTime IS NULL OR v.addTime <= :endTime)")
    List<VideoRecord> findByConditions(@Param("deviceId") String deviceId,
                                        @Param("videoName") String videoName,
                                        @Param("startTime") LocalDateTime startTime,
                                        @Param("endTime") LocalDateTime endTime);

    @Query("SELECT COUNT(v) FROM VideoRecord v WHERE " +
           "(:deviceId IS NULL OR v.deviceId = :deviceId) AND " +
           "(:videoName IS NULL OR v.videoName LIKE %:videoName%) AND " +
           "(:startTime IS NULL OR v.addTime >= :startTime) AND " +
           "(:endTime IS NULL OR v.addTime <= :endTime)")
    Long countByConditions(@Param("deviceId") String deviceId,
                          @Param("videoName") String videoName,
                          @Param("startTime") LocalDateTime startTime,
                          @Param("endTime") LocalDateTime endTime);

    List<VideoRecord> findByDeviceIdOrderByAddTimeDesc(String deviceId);

    void deleteByFilePath(String filePath);
}
