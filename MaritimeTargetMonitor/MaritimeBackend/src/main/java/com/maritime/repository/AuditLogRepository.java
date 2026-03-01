package com.maritime.repository;

import com.maritime.model.AuditLog;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;

@Repository
public interface AuditLogRepository extends JpaRepository<AuditLog, Long> {

    @Query("SELECT a FROM AuditLog a WHERE " +
           "(:operator IS NULL OR a.operator = :operator) AND " +
           "(:operationType IS NULL OR a.operationType = :operationType) AND " +
           "(:module IS NULL OR a.module = :module) AND " +
           "(:result IS NULL OR a.result = :result) AND " +
           "(:startTime IS NULL OR a.operationTime >= :startTime) AND " +
           "(:endTime IS NULL OR a.operationTime <= :endTime)")
    List<AuditLog> findByConditions(@Param("operator") String operator,
                                     @Param("operationType") String operationType,
                                     @Param("module") String module,
                                     @Param("result") String result,
                                     @Param("startTime") LocalDateTime startTime,
                                     @Param("endTime") LocalDateTime endTime);

    @Query("SELECT COUNT(a) FROM AuditLog a WHERE " +
           "(:operator IS NULL OR a.operator = :operator) AND " +
           "(:operationType IS NULL OR a.operationType = :operationType) AND " +
           "(:module IS NULL OR a.module = :module) AND " +
           "(:result IS NULL OR a.result = :result) AND " +
           "(:startTime IS NULL OR a.operationTime >= :startTime) AND " +
           "(:endTime IS NULL OR a.operationTime <= :endTime)")
    Long countByConditions(@Param("operator") String operator,
                           @Param("operationType") String operationType,
                           @Param("module") String module,
                           @Param("result") String result,
                           @Param("startTime") LocalDateTime startTime,
                           @Param("endTime") LocalDateTime endTime);

    List<AuditLog> findByOperatorOrderByOperationTimeDesc(String operator);
}
