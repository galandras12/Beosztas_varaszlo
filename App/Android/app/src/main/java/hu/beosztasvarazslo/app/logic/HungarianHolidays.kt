package hu.beosztasvarazslo.app.logic

import java.util.Calendar
import java.util.GregorianCalendar

/** Magyar munkaszüneti napok (fix + húsvéthez kötött mozgó ünnepek) számítása. */
object HungarianHolidays {

    private fun iso(year: Int, month: Int, day: Int): String =
        "%04d-%02d-%02d".format(year, month, day)

    private fun addDays(year: Int, month: Int, day: Int, delta: Int): Triple<Int, Int, Int> {
        val cal = GregorianCalendar(year, month - 1, day)
        cal.add(Calendar.DAY_OF_MONTH, delta)
        return Triple(cal.get(Calendar.YEAR), cal.get(Calendar.MONTH) + 1, cal.get(Calendar.DAY_OF_MONTH))
    }

    /** Gauss-algoritmus a húsvétvasárnap kiszámítására (Gergely-naptár). */
    private fun easterSunday(year: Int): Triple<Int, Int, Int> {
        val a = year % 19
        val b = year / 100
        val c = year % 100
        val d = b / 4
        val e = b % 4
        val f = (b + 8) / 25
        val g = (b - f + 1) / 3
        val h = (19 * a + b - d - g + 15) % 30
        val i = c / 4
        val k = c % 4
        val l = (32 + 2 * e + 2 * i - h - k) % 7
        val m = (a + 11 * h + 22 * l) / 451
        val month = (h + l - 7 * m + 114) / 31
        val day = ((h + l - 7 * m + 114) % 31) + 1
        return Triple(year, month, day)
    }

    /** Egy adott év automatikusan generált munkaszüneti napjai: dátum (ISO) -> név. */
    fun defaultHolidays(year: Int): Map<String, String> {
        val (ey, em, ed) = easterSunday(year)
        val (gy, gm, gd) = addDays(ey, em, ed, -2)   // Nagypéntek
        val (emy, emm, emd) = addDays(ey, em, ed, 1) // Húsvéthétfő
        val (wy, wm, wd) = addDays(ey, em, ed, 50)   // Pünkösdhétfő

        return linkedMapOf(
            iso(year, 1, 1) to "Újév",
            iso(year, 3, 15) to "Nemzeti ünnep (1848)",
            iso(gy, gm, gd) to "Nagypéntek",
            iso(emy, emm, emd) to "Húsvéthétfő",
            iso(year, 5, 1) to "A munka ünnepe",
            iso(wy, wm, wd) to "Pünkösdhétfő",
            iso(year, 8, 20) to "Az államalapítás ünnepe",
            iso(year, 10, 23) to "Az 1956-os forradalom ünnepe",
            iso(year, 11, 1) to "Mindenszentek",
            iso(year, 12, 25) to "Karácsony",
            iso(year, 12, 26) to "Karácsony másnapja"
        )
    }
}
