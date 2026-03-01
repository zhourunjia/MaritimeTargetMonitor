package com.maritime.repository;

import com.maritime.model.RobotHistory;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;

@Repository
public interface RobotHistoryRepository extends JpaRepository<RobotHistory, Long> {

    @Query("SELECT r FROM RobotHistory r WHERE " +
           "(:deviceId IS NULL OR r.deviceId = :deviceId) AND " +
           "(:taskId IS NULL OR r.taskId = :taskId) AND " +
           "(:taskType IS NULL OR r.taskType = :taskType) AND " +
           "(:runStatus IS NULL OR r.runStatus = :runStatus) AND " +
           "(:startTime IS NULL OR r.startTime >= :startTime) AND " +
           "(:endTime IS NULL OR r.endTime <= :endTime) AND " +
           "(:keyword IS NULL OR r.taskName LIKE %:keyword% OR r.remark LIKE %:keyword%)")
    List<RobotHistory> findByConditions(@Param("deviceId") String deviceId,
                                            @Param("taskId") String taskId,
                                            @Param("taskType") String taskType,
                                            @Param("runStatus") String runStatus,
                                            @Param("startTime") LocalDateTime startTime,
                                            @Param("endTime") LocalDateTime endTime,
                                            @Param("keyword") String keyword);

    @Query("SELECT COUNT(r) FROM RobotHistory r WHERE " +
           "(:deviceId IS NULL OR r.deviceId = :deviceId) AND " +
           "(:taskId IS NULL OR r.taskId = :taskId) AND " +
           "(:taskType IS NULL OR r.taskType = :taskType) AND " +
           "(:runStatus IS NULL OR r.runStatus = :runStatus) AND " +
           "(:startTime IS NULL OR r.startTime >= :startTime) AND " +
           "(:endTime IS NULL OR r.endTime <= :endTime) AND " +
           "(:keyword IS NULL OR r.taskName LIKE %:keyword% OR r.remark LIKE %:keyword%)")
    Long countByConditions(@Param("deviceId") String deviceId,
                             @Param("taskId") String taskId,
                             @Param("taskType") String taskType,
                             @Param("runStatus") String runStatus,
                             @Param("startTime") LocalDateTime startTime,
                             @Param("endTime") LocalDateTime endTime,
                             @Param("keyword") String keyword);

    List<RobotHistory> findByDeviceIdOrderByStartTimeDesc(String deviceId);

    List<RobotHistory> findByTaskIdOrderByStartTimeDesc(String taskId);
}
