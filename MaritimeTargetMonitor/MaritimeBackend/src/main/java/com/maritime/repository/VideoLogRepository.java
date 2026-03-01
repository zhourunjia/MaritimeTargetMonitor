package com.maritime.repository;

import com.maritime.model.VideoLog;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;

@Repository
public interface VideoLogRepository extends JpaRepository<VideoLog, Long> {

    @Query("SELECT v FROM VideoLog v WHERE " +
           "(:deviceId IS NULL OR v.deviceId = :deviceId) AND " +
           "(:videoId IS NULL OR v.videoId = :videoId) AND " +
           "(:operationType IS NULL OR v.operationType = :operationType) AND " +
           "(:operationResult IS NULL OR v.operationResult = :operationResult) AND " +
           "(:startTime IS NULL OR v.operationTime >= :startTime) AND " +
           "(:endTime IS NULL OR v.operationTime <= :endTime) AND " +
           "(:keyword IS NULL OR v.videoName LIKE %:keyword% OR v.remark LIKE %:keyword%)")
    List<VideoLog> findByConditions(@Param("deviceId") String deviceId,
                                      @Param("videoId") Long videoId,
                                      @Param("operationType") String operationType,
                                      @Param("operationResult") String operationResult,
                                      @Param("startTime") LocalDateTime startTime,
                                      @Param("endTime") LocalDateTime endTime,
                                      @Param("keyword") String keyword);

    @Query("SELECT COUNT(v) FROM VideoLog v WHERE " +
           "(:deviceId IS NULL OR v.deviceId = :deviceId) AND " +
           "(:videoId IS NULL OR v.videoId = :videoId) AND " +
           "(:operationType IS NULL OR v.operationType = :operationType) AND " +
           "(:operationResult IS NULL OR v.operationResult = :operationResult) AND " +
           "(:startTime IS NULL OR v.operationTime >= :startTime) AND " +
           "(:endTime IS NULL OR v.operationTime <= :endTime) AND " +
           "(:keyword IS NULL OR v.videoName LIKE %:keyword% OR v.remark LIKE %:keyword%)")
    Long countByConditions(@Param("deviceId") String deviceId,
                           @Param("videoId") Long videoId,
                           @Param("operationType") String operationType,
                           @Param("operationResult") String operationResult,
                           @Param("startTime") LocalDateTime startTime,
                           @Param("endTime") LocalDateTime endTime,
                           @Param("keyword") String keyword);

    List<VideoLog> findByDeviceIdOrderByOperationTimeDesc(String deviceId);

    List<VideoLog> findByVideoIdOrderByOperationTimeDesc(Long videoId);
}
