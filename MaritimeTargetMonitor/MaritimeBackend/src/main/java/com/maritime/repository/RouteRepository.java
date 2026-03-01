package com.maritime.repository;

import com.maritime.model.Route;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface RouteRepository extends JpaRepository<Route, Long>, JpaSpecificationExecutor<Route> {

    @Query("SELECT r FROM Route r WHERE " +
           "(:routeName IS NULL OR r.routeName LIKE %:routeName%) AND " +
           "(:creator IS NULL OR r.creator = :creator)")
    List<Route> findByConditions(@Param("routeName") String routeName,
                                    @Param("creator") String creator);

    @Query("SELECT COUNT(r) FROM Route r WHERE " +
           "(:routeName IS NULL OR r.routeName LIKE %:routeName%) AND " +
           "(:creator IS NULL OR r.creator = :creator)")
    Long countByConditions(@Param("routeName") String routeName,
                             @Param("creator") String creator);

    List<Route> findByCreatorOrderByCreatedAtDesc(String creator);


}
