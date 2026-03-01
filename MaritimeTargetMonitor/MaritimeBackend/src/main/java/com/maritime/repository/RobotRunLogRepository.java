package com.maritime.repository;

import com.maritime.model.RobotRunLog;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;

@Repository
public interface RobotRunLogRepository extends JpaRepository<RobotRunLog, Long> {

    @Query("SELECT r FROM RobotRunLog r WHERE " +
           "(:deviceId IS NULL OR r.deviceId = :deviceId) AND " +
           "(:taskId IS NULL OR r.taskId = :taskId) AND " +
           "(:runStatus IS NULL OR r.runStatus = :runStatus) AND " +
           "(:startTime IS NULL OR r.startTime >= :startTime) AND " +
           "(:endTime IS NULL OR r.endTime <= :endTime) AND " +
           "(:keyword IS NULL OR r.taskName LIKE %:keyword% OR r.runContent LIKE %:keyword%)")
    List<RobotRunLog> findByConditions(@Param("deviceId") String deviceId,
                                         @Param("taskId") String taskId,
                                         @Param("runStatus") String runStatus,
                                         @Param("startTime") LocalDateTime startTime,
                                         @Param("endTime") LocalDateTime endTime,
                                         @Param("keyword") String keyword);

    @Query("SELECT COUNT(r) FROM RobotRunLog r WHERE " +
           "(:deviceId IS NULL OR r.deviceId = :deviceId) AND " +
           "(:taskId IS NULL OR r.taskId = :taskId) AND " +
           "(:runStatus IS NULL OR r.runStatus = :runStatus) AND " +
           "(:startTime IS NULL OR r.startTime >= :startTime) AND " +
           "(:endTime IS NULL OR r.endTime <= :endTime) AND " +
           "(:keyword IS NULL OR r.taskName LIKE %:keyword% OR r.runContent LIKE %:keyword%)")
    Long countByConditions(@Param("deviceId") String deviceId,
                            @Param("taskId") String taskId,
                            @Param("runStatus") String runStatus,
                            @Param("startTime") LocalDateTime startTime,
                            @Param("endTime") LocalDateTime endTime,
                            @Param("keyword") String keyword);

    List<RobotRunLog> findByDeviceIdOrderByStartTimeDesc(String deviceId);

    List<RobotRunLog> findByTaskIdOrderByStartTimeDesc(String taskId);
}
