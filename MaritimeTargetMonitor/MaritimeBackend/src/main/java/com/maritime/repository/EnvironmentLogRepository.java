package com.maritime.repository;

import com.maritime.model.EnvironmentLog;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;

@Repository
public interface EnvironmentLogRepository extends JpaRepository<EnvironmentLog, Long> {

    @Query("SELECT e FROM EnvironmentLog e WHERE " +
           "(:deviceId IS NULL OR e.deviceId = :deviceId) AND " +
           "(:startTime IS NULL OR e.recordTime >= :startTime) AND " +
           "(:endTime IS NULL OR e.recordTime <= :endTime) AND " +
           "(:keyword IS NULL OR e.remark LIKE %:keyword%)")
    List<EnvironmentLog> findByConditions(@Param("deviceId") String deviceId,
                                             @Param("startTime") LocalDateTime startTime,
                                             @Param("endTime") LocalDateTime endTime,
                                             @Param("keyword") String keyword);

    @Query("SELECT COUNT(e) FROM EnvironmentLog e WHERE " +
           "(:deviceId IS NULL OR e.deviceId = :deviceId) AND " +
           "(:startTime IS NULL OR e.recordTime >= :startTime) AND " +
           "(:endTime IS NULL OR e.recordTime <= :endTime) AND " +
           "(:keyword IS NULL OR e.remark LIKE %:keyword%)")
    Long countByConditions(@Param("deviceId") String deviceId,
                            @Param("startTime") LocalDateTime startTime,
                            @Param("endTime") LocalDateTime endTime,
                            @Param("keyword") String keyword);

    List<EnvironmentLog> findByDeviceIdOrderByRecordTimeDesc(String deviceId);
}
