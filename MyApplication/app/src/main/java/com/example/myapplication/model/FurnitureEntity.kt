package com.example.myapplication.model

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "furniture_table")
data class FurnitureEntity(
    @PrimaryKey(autoGenerate = true)
    val id: Int = 0,
    val name: String,
    val widthCm: Float,
    val depthCm: Float,
    val heightCm: Float,
    val xOffsetDp: Float = 100f, // 도면 배치 X 좌표 (기본값)
    val yOffsetDp: Float = 100f, // 도면 배치 Y 좌표 (기본값)
    val isNew: Boolean = true,   // 신규 추가 가구 여부
    val createdAt: Long = System.currentTimeMillis() // 생성 날짜 (정렬용)
)

