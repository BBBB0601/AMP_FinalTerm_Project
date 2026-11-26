package com.example.myapplication

import android.content.Context
import android.content.Intent
import android.graphics.Color
import android.graphics.drawable.GradientDrawable
import android.os.Build
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.os.VibrationEffect
import android.os.Vibrator
import android.os.VibratorManager
import android.util.TypedValue
import android.view.Gravity
import android.view.View
import android.widget.Button
import android.widget.FrameLayout
import android.widget.LinearLayout
import android.widget.TextView
import androidx.annotation.Keep
import com.unity3d.player.UnityPlayer
import com.unity3d.player.UnityPlayerActivity
import org.json.JSONObject

/**
 * [SFR-002 / SCR-002] Unity as a Library (UaaL) AR 실행 Activity.
 * - Android -> Unity: UnityPlayer.UnitySendMessage("ARBridgeManager", ...)
 * - Unity -> Android: JNI currentActivity.Call("OnCollisionStateChanged" / "OnCollisionEventReceived")
 */
class RoomFitARActivity : UnityPlayerActivity() {

    companion object {
        const val EXTRA_FURNITURE_JSON = "extra_furniture_json"
        const val EXTRA_FURNITURE_NAME = "extra_furniture_name"
        private const val UNITY_BRIDGE_OBJECT = "ARBridgeManager"
        private const val INITIAL_SEND_DELAY_MS = 1200L

        fun createIntent(context: Context, furnitureName: String, furnitureJson: String): Intent {
            return Intent(context, RoomFitARActivity::class.java).apply {
                putExtra(EXTRA_FURNITURE_NAME, furnitureName)
                putExtra(EXTRA_FURNITURE_JSON, furnitureJson)
            }
        }
    }

    private val mainHandler = Handler(Looper.getMainLooper())
    private var pendingFurnitureJson: String? = null
    private var hasSentInitialFurniture = false
    private var isClearanceGuideOn = true
    private var isCurrentlyColliding = false

    private lateinit var warningBanner: TextView
    private lateinit var titleBadge: TextView
    private lateinit var guideToggleButton: Button

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        pendingFurnitureJson = intent.getStringExtra(EXTRA_FURNITURE_JSON)
        val furnitureName = intent.getStringExtra(EXTRA_FURNITURE_NAME).orEmpty().ifBlank { "가구" }

        attachNativeOverlay(furnitureName)
    }

    override fun onResume() {
        super.onResume()
        if (!hasSentInitialFurniture && !pendingFurnitureJson.isNullOrBlank()) {
            hasSentInitialFurniture = true
            val json = pendingFurnitureJson!!
            mainHandler.postDelayed({
                sendToUnity("OnReceiveFurnitureData", json)
            }, INITIAL_SEND_DELAY_MS)
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        val json = intent.getStringExtra(EXTRA_FURNITURE_JSON)
        val name = intent.getStringExtra(EXTRA_FURNITURE_NAME)
        if (!name.isNullOrBlank()) {
            titleBadge.text = name
        }
        if (!json.isNullOrBlank()) {
            pendingFurnitureJson = json
            sendToUnity("OnReceiveFurnitureData", json)
        }
    }

    override fun onDestroy() {
        mainHandler.removeCallbacksAndMessages(null)
        super.onDestroy()
    }

    private fun attachNativeOverlay(furnitureName: String) {
        val rootLayout = mUnityPlayer.frameLayout

        val overlayContainer = FrameLayout(this).apply {
            layoutParams = FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
            )
            isClickable = false
            isFocusable = false
        }

        // 상단 바 (뒤로가기 + 현재 가구명 + 충돌 경고 배너)
        val topColumn = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            layoutParams = FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.WRAP_CONTENT,
                Gravity.TOP
            ).apply {
                topMargin = dp(40)
                leftMargin = dp(16)
                rightMargin = dp(16)
            }
        }

        val headerRow = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = Gravity.CENTER_VERTICAL
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            )
        }

        val backButton = createOverlayButton("← 뒤로", Color.parseColor("#CC1F2937")) {
            finish()
        }

        titleBadge = TextView(this).apply {
            text = furnitureName
            setTextColor(Color.WHITE)
            setTextSize(TypedValue.COMPLEX_UNIT_SP, 14f)
            setPadding(dp(14), dp(8), dp(14), dp(8))
            background = createRoundedBg(Color.parseColor("#B3111827"), dp(10))
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply {
                leftMargin = dp(10)
            }
        }

        headerRow.addView(backButton)
        headerRow.addView(titleBadge)
        topColumn.addView(headerRow)

        warningBanner = TextView(this).apply {
            text = "벽면 침범 발생"
            setTextColor(Color.WHITE)
            setTextSize(TypedValue.COMPLEX_UNIT_SP, 15f)
            gravity = Gravity.CENTER
            setPadding(dp(16), dp(10), dp(16), dp(10))
            background = createRoundedBg(Color.parseColor("#E6DC2626"), dp(10))
            visibility = View.GONE
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply {
                topMargin = dp(10)
            }
        }
        topColumn.addView(warningBanner)
        overlayContainer.addView(topColumn)

        // 하단 AR 제어 툴바 (SCR-002)
        val bottomToolbar = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = Gravity.CENTER
            layoutParams = FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.WRAP_CONTENT,
                Gravity.BOTTOM
            ).apply {
                bottomMargin = dp(20)
                leftMargin = dp(12)
                rightMargin = dp(12)
            }
        }

        val btnPlace = createToolbarButton("위치 확정", Color.parseColor("#E60D9488")) {
            sendToUnity("OnPlaceFurniture", "")
        }

        guideToggleButton = createToolbarButton("동선 ON", Color.parseColor("#E6D97706")) {
            isClearanceGuideOn = !isClearanceGuideOn
            guideToggleButton.text = if (isClearanceGuideOn) "동선 ON" else "동선 OFF"
            sendToUnity("OnToggleClearanceGuide", isClearanceGuideOn.toString())
        }

        val btnCancel = createToolbarButton("취소", Color.parseColor("#E64B5563")) {
            sendToUnity("OnCancelFurniture", "")
        }

        val btnReset = createToolbarButton("초기화", Color.parseColor("#E6991B1B")) {
            sendToUnity("OnResetPlacement", "")
        }

        bottomToolbar.addView(btnPlace)
        bottomToolbar.addView(guideToggleButton)
        bottomToolbar.addView(btnCancel)
        bottomToolbar.addView(btnReset)
        overlayContainer.addView(bottomToolbar)

        rootLayout.addView(overlayContainer)
    }

    private fun sendToUnity(methodName: String, message: String) {
        try {
            UnityPlayer.UnitySendMessage(UNITY_BRIDGE_OBJECT, methodName, message)
        } catch (_: Throwable) {
            // Unity 런타임 초기화 전 예외 방지
        }
    }

    // ---------- Unity -> Android JNI 콜백 (ARBridgeManager.cs에서 호출) ----------

    @Keep
    @Suppress("unused")
    fun OnCollisionStateChanged(isColliding: Boolean) {
        updateCollisionUi(isColliding, if (isColliding) "벽면 침범 발생" else "배치 가능 영역")
    }

    @Keep
    @Suppress("unused")
    fun OnCollisionEventReceived(jsonPayload: String) {
        try {
            val json = JSONObject(jsonPayload)
            val isColliding = json.optBoolean("isColliding", false)
            val message = json.optString("message", if (isColliding) "벽면 침범 발생" else "배치 가능 영역")
            updateCollisionUi(isColliding, message)
        } catch (_: Throwable) {
            // JSON 파싱 실패 시 무시
        }
    }

    private fun updateCollisionUi(isColliding: Boolean, message: String) {
        runOnUiThread {
            val shouldVibrate = isColliding && !isCurrentlyColliding
            isCurrentlyColliding = isColliding

            if (isColliding) {
                warningBanner.text = message.ifBlank { "벽면 침범 발생" }
                warningBanner.visibility = View.VISIBLE
                if (shouldVibrate) {
                    triggerCollisionHaptic()
                }
            } else {
                warningBanner.visibility = View.GONE
            }
        }
    }

    private fun triggerCollisionHaptic() {
        try {
            val vibrator = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                val manager = getSystemService(Context.VIBRATOR_MANAGER_SERVICE) as? VibratorManager
                manager?.defaultVibrator
            } else {
                @Suppress("DEPRECATION")
                getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator
            }
            vibrator?.vibrate(VibrationEffect.createOneShot(80L, VibrationEffect.DEFAULT_AMPLITUDE))
        } catch (_: Throwable) {
            // 진동 미지원 단말 무시
        }
    }

    private fun createOverlayButton(label: String, bgColor: Int, onClick: () -> Unit): Button {
        return Button(this).apply {
            text = label
            setTextColor(Color.WHITE)
            setTextSize(TypedValue.COMPLEX_UNIT_SP, 13f)
            isAllCaps = false
            setPadding(dp(12), dp(6), dp(12), dp(6))
            minHeight = 0
            minimumHeight = 0
            background = createRoundedBg(bgColor, dp(10))
            setOnClickListener { onClick() }
        }
    }

    private fun createToolbarButton(label: String, bgColor: Int, onClick: () -> Unit): Button {
        return createOverlayButton(label, bgColor, onClick).apply {
            layoutParams = LinearLayout.LayoutParams(
                0,
                dp(44),
                1f
            ).apply {
                leftMargin = dp(4)
                rightMargin = dp(4)
            }
        }
    }

    private fun createRoundedBg(color: Int, radiusPx: Int): GradientDrawable {
        return GradientDrawable().apply {
            setColor(color)
            cornerRadius = radiusPx.toFloat()
        }
    }

    private fun dp(value: Int): Int {
        return TypedValue.applyDimension(
            TypedValue.COMPLEX_UNIT_DIP,
            value.toFloat(),
            resources.displayMetrics
        ).toInt()
    }
}
