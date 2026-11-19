package com.example.myapplication.model

import androidx.room.*
import kotlinx.coroutines.flow.Flow

@Dao
interface FurnitureDao {
    // 모든 가구 목록 조회 (최신순 정렬, Flow로 실시간 업데이트 감지)
    @Query("SELECT * FROM furniture_table ORDER BY createdAt DESC")
    fun getAllFurniture(): Flow<List<FurnitureEntity>>

    // 가구 데이터 추가
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertFurniture(furniture: FurnitureEntity)

    // 특정 가구 삭제
    @Delete
    suspend fun deleteFurniture(furniture: FurnitureEntity)

    // 전체 가구 데이터 삭제 (초기화용)
    @Query("DELETE FROM furniture_table")
    suspend fun deleteAll()
}