package hu.beosztasvarazslo.app.logic

import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.pdf.PdfDocument
import java.io.File
import java.io.FileOutputStream

data class PrintRow(
    val label: String,
    val isGroupHeader: Boolean,
    val dayTexts: List<String>,
    val balanceText: String
)

data class PrintDocument(
    val title: String,
    val dayHeaderLabels: List<String>,
    val rows: List<PrintRow>
)

/**
 * A4 fekvő elrendezésű, nyomtatható táblázat előállítása PDF-be (több oldalas, lapozott)
 * és egyetlen folytonos JPEG képbe. Nem függ az adatrétegtől - kész PrintDocument-et vár.
 */
object PrintRenderer {

    fun generatePdf(context: Context, doc: PrintDocument, fileName: String): File {
        val pdf = PdfDocument()
        val pageWidth = 842 // A4 fekvő, pt (72 dpi)
        val pageHeight = 595
        val marginX = 24f
        val marginY = 20f
        val titleHeight = 22f
        val headerRowHeight = 26f
        val rowHeight = 14f
        val nameColWidth = 120f
        val balanceColWidth = 55f
        val usableWidth = pageWidth - 2 * marginX
        val dayCount = doc.dayHeaderLabels.size.coerceAtLeast(1)
        val dayColWidth = (usableWidth - nameColWidth - balanceColWidth) / dayCount
        val usableHeight = pageHeight - 2 * marginY - titleHeight - headerRowHeight
        val rowsPerPage = (usableHeight / rowHeight).toInt().coerceAtLeast(1)

        val titlePaint = Paint().apply { textSize = 12f; isFakeBoldText = true; color = Color.BLACK }
        val headerPaint = Paint().apply { textSize = 7f; isFakeBoldText = true; color = Color.BLACK; textAlign = Paint.Align.CENTER }
        val cellPaint = Paint().apply { textSize = 6.5f; color = Color.BLACK; textAlign = Paint.Align.CENTER }
        val labelPaint = Paint().apply { textSize = 7f; color = Color.BLACK }
        val groupPaint = Paint().apply { textSize = 7.5f; isFakeBoldText = true; color = Color.BLACK }
        val gridPaint = Paint().apply { color = Color.rgb(60, 60, 60); style = Paint.Style.STROKE; strokeWidth = 0.5f }
        val headerBgPaint = Paint().apply { color = Color.rgb(224, 224, 224); style = Paint.Style.FILL }
        val groupBgPaint = Paint().apply { color = Color.rgb(210, 210, 210); style = Paint.Style.FILL }

        var rowIdx = 0
        var pageNum = 1
        while (rowIdx < doc.rows.size || pageNum == 1) {
            val pageInfo = PdfDocument.PageInfo.Builder(pageWidth, pageHeight, pageNum).create()
            val page = pdf.startPage(pageInfo)
            val canvas = page.canvas

            var y = marginY
            canvas.drawText("${doc.title}  –  ${pageNum}. oldal", marginX, y + titleHeight - 6, titlePaint)
            y += titleHeight

            // Napok fejléc
            var x = marginX
            canvas.drawRect(x, y, x + nameColWidth, y + headerRowHeight, headerBgPaint)
            canvas.drawRect(x, y, x + nameColWidth, y + headerRowHeight, gridPaint)
            x += nameColWidth
            doc.dayHeaderLabels.forEach { label ->
                canvas.drawRect(x, y, x + dayColWidth, y + headerRowHeight, headerBgPaint)
                canvas.drawRect(x, y, x + dayColWidth, y + headerRowHeight, gridPaint)
                val lines = label.split("\n")
                lines.forEachIndexed { i, line ->
                    canvas.drawText(line, x + dayColWidth / 2, y + headerRowHeight / 2 - 2 + i * 8f, headerPaint)
                }
                x += dayColWidth
            }
            canvas.drawRect(x, y, x + balanceColWidth, y + headerRowHeight, headerBgPaint)
            canvas.drawRect(x, y, x + balanceColWidth, y + headerRowHeight, gridPaint)
            canvas.drawText("Köv.hóra", x + balanceColWidth / 2, y + headerRowHeight / 2 - 2, headerPaint)
            canvas.drawText("átvitt óra", x + balanceColWidth / 2, y + headerRowHeight / 2 + 8, headerPaint)
            y += headerRowHeight

            var rowsOnPage = 0
            while (rowIdx < doc.rows.size && rowsOnPage < rowsPerPage) {
                val row = doc.rows[rowIdx]
                x = marginX
                if (row.isGroupHeader) {
                    val fullWidth = nameColWidth + dayCount * dayColWidth + balanceColWidth
                    canvas.drawRect(x, y, x + fullWidth, y + rowHeight, groupBgPaint)
                    canvas.drawRect(x, y, x + fullWidth, y + rowHeight, gridPaint)
                    canvas.drawText(row.label, x + 4, y + rowHeight - 4, groupPaint)
                } else {
                    canvas.drawRect(x, y, x + nameColWidth, y + rowHeight, gridPaint)
                    canvas.drawText(row.label, x + 3, y + rowHeight - 4, labelPaint)
                    x += nameColWidth
                    row.dayTexts.forEach { text ->
                        canvas.drawRect(x, y, x + dayColWidth, y + rowHeight, gridPaint)
                        if (text.isNotEmpty()) canvas.drawText(text, x + dayColWidth / 2, y + rowHeight - 4, cellPaint)
                        x += dayColWidth
                    }
                    canvas.drawRect(x, y, x + balanceColWidth, y + rowHeight, gridPaint)
                    canvas.drawText(row.balanceText, x + balanceColWidth / 2, y + rowHeight - 4, cellPaint)
                }
                y += rowHeight
                rowIdx++
                rowsOnPage++
            }

            pdf.finishPage(page)
            if (rowIdx >= doc.rows.size) break
            pageNum++
        }

        val dir = File(context.getExternalFilesDir(null), "exports").apply { mkdirs() }
        val file = File(dir, fileName)
        FileOutputStream(file).use { pdf.writeTo(it) }
        pdf.close()
        return file
    }

    fun generateJpeg(context: Context, doc: PrintDocument, fileName: String, quality: Int = 92): File {
        val scale = 3f
        val nameColWidth = 130f * scale
        val balanceColWidth = 70f * scale
        val dayColWidth = 26f * scale
        val titleHeight = 34f * scale
        val headerRowHeight = 34f * scale
        val rowHeight = 20f * scale
        val margin = 10f * scale

        val dayCount = doc.dayHeaderLabels.size.coerceAtLeast(1)
        val width = (margin * 2 + nameColWidth + dayCount * dayColWidth + balanceColWidth).toInt()
        val height = (margin * 2 + titleHeight + headerRowHeight + doc.rows.size * rowHeight).toInt().coerceAtLeast(200)

        val bitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)
        canvas.drawColor(Color.WHITE)

        val titlePaint = Paint().apply { textSize = 16f * scale; isFakeBoldText = true; color = Color.BLACK }
        val headerPaint = Paint().apply { textSize = 8f * scale; isFakeBoldText = true; color = Color.BLACK; textAlign = Paint.Align.CENTER }
        val cellPaint = Paint().apply { textSize = 7.5f * scale; color = Color.BLACK; textAlign = Paint.Align.CENTER }
        val labelPaint = Paint().apply { textSize = 8f * scale; color = Color.BLACK }
        val groupPaint = Paint().apply { textSize = 8.5f * scale; isFakeBoldText = true; color = Color.BLACK }
        val gridPaint = Paint().apply { color = Color.rgb(60, 60, 60); style = Paint.Style.STROKE; strokeWidth = 1f }
        val headerBgPaint = Paint().apply { color = Color.rgb(224, 224, 224); style = Paint.Style.FILL }
        val groupBgPaint = Paint().apply { color = Color.rgb(210, 210, 210); style = Paint.Style.FILL }

        var y = margin
        canvas.drawText(doc.title, margin, y + titleHeight - 8 * scale, titlePaint)
        y += titleHeight

        var x = margin
        canvas.drawRect(x, y, x + nameColWidth, y + headerRowHeight, headerBgPaint)
        canvas.drawRect(x, y, x + nameColWidth, y + headerRowHeight, gridPaint)
        x += nameColWidth
        doc.dayHeaderLabels.forEach { label ->
            canvas.drawRect(x, y, x + dayColWidth, y + headerRowHeight, headerBgPaint)
            canvas.drawRect(x, y, x + dayColWidth, y + headerRowHeight, gridPaint)
            label.split("\n").forEachIndexed { i, line ->
                canvas.drawText(line, x + dayColWidth / 2, y + headerRowHeight / 2 - 2 * scale + i * 9f * scale, headerPaint)
            }
            x += dayColWidth
        }
        canvas.drawRect(x, y, x + balanceColWidth, y + headerRowHeight, headerBgPaint)
        canvas.drawRect(x, y, x + balanceColWidth, y + headerRowHeight, gridPaint)
        canvas.drawText("Köv.hóra átvitt", x + balanceColWidth / 2, y + headerRowHeight / 2 + 3 * scale, headerPaint)
        y += headerRowHeight

        doc.rows.forEach { row ->
            x = margin
            if (row.isGroupHeader) {
                val fullWidth = nameColWidth + dayCount * dayColWidth + balanceColWidth
                canvas.drawRect(x, y, x + fullWidth, y + rowHeight, groupBgPaint)
                canvas.drawRect(x, y, x + fullWidth, y + rowHeight, gridPaint)
                canvas.drawText(row.label, x + 6 * scale, y + rowHeight - 6 * scale, groupPaint)
            } else {
                canvas.drawRect(x, y, x + nameColWidth, y + rowHeight, gridPaint)
                canvas.drawText(row.label, x + 5 * scale, y + rowHeight - 6 * scale, labelPaint)
                x += nameColWidth
                row.dayTexts.forEach { text ->
                    canvas.drawRect(x, y, x + dayColWidth, y + rowHeight, gridPaint)
                    if (text.isNotEmpty()) canvas.drawText(text, x + dayColWidth / 2, y + rowHeight - 6 * scale, cellPaint)
                    x += dayColWidth
                }
                canvas.drawRect(x, y, x + balanceColWidth, y + rowHeight, gridPaint)
                canvas.drawText(row.balanceText, x + balanceColWidth / 2, y + rowHeight - 6 * scale, cellPaint)
            }
            y += rowHeight
        }

        val dir = File(context.getExternalFilesDir(null), "exports").apply { mkdirs() }
        val file = File(dir, fileName)
        FileOutputStream(file).use { bitmap.compress(Bitmap.CompressFormat.JPEG, quality, it) }
        bitmap.recycle()
        return file
    }
}
