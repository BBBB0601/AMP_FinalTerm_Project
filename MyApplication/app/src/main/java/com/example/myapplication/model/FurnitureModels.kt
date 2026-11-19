package com.example.myapplication.model

data class FurnitureConfig(
    val name: String,
    val widthCm: Float,
    val depthCm: Float,
    val heightCm: Float
) {
    val widthM: Float get() = widthCm / 100f
    val depthM: Float get() = depthCm / 100f
    val heightM: Float get() = heightCm / 100f
}

data class PlacedFurniture(
    val id: String,
    val name: String,
    val widthCm: Float,
    val depthCm: Float,
    val xOffsetDp: Float,
    val yOffsetDp: Float,
    val isNew: Boolean = false
)

data class FurniturePreset(val name: String, val w: Float, val d: Float, val h: Float)

val PRESETS = listOf(
    FurniturePreset("책상", 120f, 60f, 75f),
    FurniturePreset("3단 서랍장", 80f, 40f, 90f),
    FurniturePreset("소형 냉장고", 47f, 50f, 85f)
)