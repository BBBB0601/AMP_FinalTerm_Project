package com.example.myapplication.ui

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.model.FurnitureEntity
import com.example.myapplication.ui.theme.*

@Composable
fun FurnitureReportCard(furnitureList: List<FurnitureEntity>) {
    val roomAreaM2 = 12.0f
    val totalFurnitureAreaM2 = furnitureList.sumOf {
        (it.widthCm / 100.0) * (it.depthCm / 100.0)
    }.toFloat()

    val totalVolumeM3 = furnitureList.sumOf {
        (it.widthCm / 100.0) * (it.depthCm / 100.0) * (it.heightCm / 100.0)
    }.toFloat()

    val occupancyRate = ((totalFurnitureAreaM2 / roomAreaM2) * 100).coerceAtMost(100f)

    val (statusText, statusColor) = when {
        occupancyRate <= 30f -> "동선 쾌적 (통행 원활)" to Color(0xFF2E7D32)
        occupancyRate <= 50f -> "적정 배치 (Clearance 60cm 확보)" to PurplePrimary
        else -> "공간 협소 (문열림/동선 간섭 주의)" to Color(0xFFE74C3C)
    }

    Card(
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = CardGrayBg),
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 12.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text("📊 가구 배치 분석 리포트", fontSize = 15.sp, fontWeight = FontWeight.Bold, color = TextDark)
                Surface(
                    shape = RoundedCornerShape(6.dp),
                    color = statusColor.copy(alpha = 0.15f)
                ) {
                    Text(
                        statusText,
                        fontSize = 11.sp,
                        fontWeight = FontWeight.Bold,
                        color = statusColor,
                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp)
                    )
                }
            }

            Spacer(modifier = Modifier.height(12.dp))

            Column {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text("원룸 면적 점유율", fontSize = 12.sp, color = TextMuted)
                    Text("${String.format("%.1f", occupancyRate)}% / 100%", fontSize = 12.sp, fontWeight = FontWeight.Bold, color = TextDark)
                }
                Spacer(modifier = Modifier.height(6.dp))
                LinearProgressIndicator(
                    progress = (occupancyRate / 100f).coerceIn(0f, 1f),
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(8.dp)
                        .clip(RoundedCornerShape(4.dp)),
                    color = statusColor,
                    trackColor = BorderLight
                )
            }

            Spacer(modifier = Modifier.height(12.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Surface(
                    modifier = Modifier.weight(1f),
                    shape = RoundedCornerShape(10.dp),
                    color = Color.White
                ) {
                    Column(
                        modifier = Modifier.padding(10.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text("총 점유 면적", fontSize = 11.sp, color = TextMuted)
                        Spacer(modifier = Modifier.height(2.dp))
                        Text("${String.format("%.2f", totalFurnitureAreaM2)} ㎡", fontSize = 14.sp, fontWeight = FontWeight.Bold, color = TextDark)
                    }
                }
                Surface(
                    modifier = Modifier.weight(1f),
                    shape = RoundedCornerShape(10.dp),
                    color = Color.White
                ) {
                    Column(
                        modifier = Modifier.padding(10.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text("총 체적 (부피)", fontSize = 11.sp, color = TextMuted)
                        Spacer(modifier = Modifier.height(2.dp))
                        Text("${String.format("%.2f", totalVolumeM3)} ㎥", fontSize = 14.sp, fontWeight = FontWeight.Bold, color = TextDark)
                    }
                }
            }
        }
    }
}