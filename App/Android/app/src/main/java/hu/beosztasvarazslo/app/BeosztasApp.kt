package hu.beosztasvarazslo.app

import android.app.Application
import hu.beosztasvarazslo.app.data.AppDatabase
import hu.beosztasvarazslo.app.data.AppRepository

class BeosztasApp : Application() {
    val database: AppDatabase by lazy { AppDatabase.getInstance(this) }
    val repository: AppRepository by lazy { AppRepository(database) }
}
