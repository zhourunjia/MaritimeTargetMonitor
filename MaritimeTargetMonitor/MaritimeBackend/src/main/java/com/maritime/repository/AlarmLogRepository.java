package com.maritime.repository;

import com.maritime.model.AlarmLog;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;

@Repository
public interface AlarmLogRepository extends JpaRepository<AlarmLog, Long> {

    @Query("SELECT a FROM AlarmLog a WHERE " +
           "(:deviceId IS NULL OR a.deviceId = :deviceId) AND " +
           "(:alarmType IS NULL OR a.alarmType = :alarmType) AND " +
           "(:alarmLevel IS NULL OR a.alarmLevel = :alarmLevel) AND " +
           "(:startTime IS NULL OR a.alarmTime >= :startTime) AND " +
           "(:endTime IS NULL OR a.alarmTime <= :endTime) AND " +
           "(:keyword IS NULL OR a.alarmContent LIKE %:keyword%)")
    List<AlarmLog> findByConditions(@Param("deviceId") String deviceId,
                                     @Param("alarmType") String alarmType,
                                     @Param("alarmLevel") String alarmLevel,
                                     @Param("startTime") LocalDateTime startTime,
                                     @Param("endTime") LocalDateTime endTime,
                                     @Param("keyword") String keyword);

    @Query("SELECT COUNT(a) FROM AlarmLog a WHERE " +
           "(:deviceId IS NULL OR a.deviceId = :deviceId) AND " +
           "(:alarmType IS NULL OR a.alarmType = :alarmType) AND " +
           "(:alarmLevel IS NULL OR a.alarmLevel = :alarmLevel) AND " +
           "(:startTime IS NULL OR a.alarmTime >= :startTime) AND " +
           "(:endTime IS NULL OR a.alarmTime <= :endTime) AND " +
           "(:keyword IS NULL OR a.alarmContent LIKE %:keyword%)")
    Long countByConditions(@Param("deviceId") String deviceId,
                           @Param("alarmType") String alarmType,
                           @Param("alarmLevel") String alarmLevel,
                           @Param("startTime") LocalDateTime startTime,
                           @Param("endTime") LocalDateTime endTime,
                           @Param("keyword") String keyword);

    List<AlarmLog> findByDeviceIdOrderByAlarmTimeDesc(String deviceId);

    List<AlarmLog> findByIsHandledFalseOrderByAlarmTimeDesc();
}
