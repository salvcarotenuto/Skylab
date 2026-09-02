package it.skylab.mobile

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.BackHandler
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarDuration
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.DpOffset
import androidx.compose.ui.unit.sp
import it.skylab.mobile.ui.theme.SkyLabMobileTheme
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL
import java.text.NumberFormat
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            SkyLabMobileTheme {
                Scaffold(modifier = Modifier.fillMaxSize()) { innerPadding ->
                    LoginScreen(modifier = Modifier.padding(innerPadding))
                }
            }
        }
    }
}

@Composable
fun LoginScreen(
    modifier: Modifier = Modifier,
    loadUsers: suspend () -> List<String> = ::loadLoginUsers
) {
    val context = LocalContext.current
    val sessionPreferences = remember(context) { context.getSharedPreferences("skylab-session", android.content.Context.MODE_PRIVATE) }
    var username by remember { mutableStateOf(sessionPreferences.getString("username", "").orEmpty()) }
    var password by remember { mutableStateOf("") }
    var passwordVisible by remember { mutableStateOf(false) }
    var menuExpanded by remember { mutableStateOf(false) }
    var users by remember { mutableStateOf<List<String>>(emptyList()) }
    var loadingUsers by remember { mutableStateOf(true) }
    var usersError by remember { mutableStateOf(false) }
    var loginInProgress by remember { mutableStateOf(false) }
    var loginError by remember { mutableStateOf(false) }
    var loggedIn by remember { mutableStateOf(username.isNotBlank()) }
    var sessionToken by remember { mutableStateOf(sessionPreferences.getString("token", "").orEmpty()) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(Unit) {
        if (loggedIn) {
            loadingUsers = false
            return@LaunchedEffect
        }
        try {
            users = loadUsers()
        } catch (exception: Exception) {
            usersError = true
            android.util.Log.e("SkyLabMobile", "Caricamento utenti non riuscito", exception)
        } finally {
            loadingUsers = false
        }
    }

    if (loggedIn) {
        WelcomeScreen(
            username = username,
            token = sessionToken,
            onLogout = {
                sessionPreferences.edit().clear().apply()
                username = ""
                sessionToken = ""
                password = ""
                users = emptyList()
                loadingUsers = true
                loggedIn = false
                usersError = false
                scope.launch {
                    try {
                        users = loadUsers()
                    } catch (exception: Exception) {
                        usersError = true
                        android.util.Log.e("SkyLabMobile", "Caricamento utenti non riuscito", exception)
                    } finally {
                        loadingUsers = false
                    }
                }
            },
            modifier = modifier
        )
        return
    }

    Column(
        modifier = modifier.fillMaxSize().imePadding().padding(horizontal = 32.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = "SkyLab Mobile",
            style = MaterialTheme.typography.headlineLarge,
            fontSize = 30.sp,
            fontWeight = FontWeight.Bold
        )
        Text(
            text = "Accedi al tuo account",
            style = MaterialTheme.typography.bodyLarge,
            fontSize = 17.sp,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(modifier = Modifier.height(32.dp))
        Box(modifier = Modifier.fillMaxWidth()) {
            OutlinedButton(
                onClick = { menuExpanded = true },
                modifier = Modifier.fillMaxWidth().height(56.dp),
                enabled = !loadingUsers && users.isNotEmpty()
            ) {
                if (loadingUsers) {
                    CircularProgressIndicator(modifier = Modifier.height(20.dp))
                } else {
                    Text(username.ifEmpty { "Login utente" }, fontSize = 20.sp)
                }
            }
            DropdownMenu(
                expanded = menuExpanded,
                onDismissRequest = { menuExpanded = false },
                modifier = Modifier.width(280.dp),
                offset = DpOffset(x = 8.dp, y = 0.dp)
            ) {
                users.forEach { user ->
                    DropdownMenuItem(
                        text = { Text(user, fontSize = 20.sp) },
                        onClick = {
                            username = user
                            menuExpanded = false
                        }
                    )
                }
            }
        }
        if (usersError) {
            Text(
                text = "Impossibile caricare gli utenti",
                color = MaterialTheme.colorScheme.error,
                style = MaterialTheme.typography.bodySmall
            )
        }
        Spacer(modifier = Modifier.height(16.dp))
        OutlinedTextField(
            value = password,
            onValueChange = { password = it },
            modifier = Modifier.fillMaxWidth(),
            label = { Text("Password", fontSize = 15.sp) },
            textStyle = MaterialTheme.typography.bodyLarge.copy(fontSize = 18.sp),
            singleLine = true,
            visualTransformation = if (passwordVisible) VisualTransformation.None
            else PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
            trailingIcon = {
                IconButton(onClick = { passwordVisible = !passwordVisible }) {
                    Icon(
                        painter = painterResource(R.drawable.ic_visibility),
                        contentDescription = if (passwordVisible) "Nascondi password" else "Mostra password"
                    )
                }
            }
        )
        Spacer(modifier = Modifier.height(24.dp))
        Button(
            onClick = {
                loginError = false
                loginInProgress = true
                scope.launch {
                    try {
                        sessionToken = authenticate(username, password).orEmpty()
                        loggedIn = sessionToken.isNotEmpty()
                        if (loggedIn) sessionPreferences.edit().putString("username", username).putString("token", sessionToken).apply()
                        loginError = !loggedIn
                    } catch (_: Exception) {
                        loginError = true
                    } finally {
                        loginInProgress = false
                    }
                }
            },
            modifier = Modifier.fillMaxWidth(),
            enabled = username.isNotBlank() && password.isNotBlank() && !loginInProgress
        ) {
            Text(if (loginInProgress) "Accesso…" else "Accedi", fontSize = 18.sp)
        }
        if (loginError) {
            Text(
                text = "Utente o password non corretti",
                color = MaterialTheme.colorScheme.error,
                style = MaterialTheme.typography.bodyMedium,
                modifier = Modifier.padding(top = 12.dp)
            )
        }
    }
}

@Composable
private fun WelcomeScreen(username: String, token: String, onLogout: () -> Unit, modifier: Modifier = Modifier) {
    var showWorks by remember { mutableStateOf(false) }
    if (showWorks) {
        MyWorksScreen(username = username, token = token, onBack = { showWorks = false }, modifier = modifier)
        return
    }
    Column(
        modifier = modifier.fillMaxSize().padding(horizontal = 32.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text("SkyLab Mobile", style = MaterialTheme.typography.headlineLarge, fontWeight = FontWeight.Bold)
        Spacer(modifier = Modifier.height(20.dp))
        Text("Benvenuto, $username", fontSize = 22.sp, fontWeight = FontWeight.SemiBold)
        Spacer(modifier = Modifier.height(8.dp))
        Text("Accesso eseguito correttamente", fontSize = 17.sp)
        Spacer(modifier = Modifier.height(36.dp))
        Button(
            onClick = { showWorks = true },
            modifier = Modifier.fillMaxWidth().height(58.dp)
        ) {
            Text("Lavori assegnati", fontSize = 20.sp)
        }
        Spacer(modifier = Modifier.height(16.dp))
        OutlinedButton(
            onClick = { },
            modifier = Modifier.fillMaxWidth().height(58.dp)
        ) {
            Text("Agenda", fontSize = 20.sp)
        }
        Spacer(modifier = Modifier.height(16.dp))
        OutlinedButton(onClick = onLogout, modifier = Modifier.fillMaxWidth().height(52.dp)) {
            Text("Cambia operatore", fontSize = 18.sp)
        }
    }
}

@Composable
private fun MyWorksScreen(username: String, token: String, onBack: () -> Unit, modifier: Modifier = Modifier) {
    var works by remember { mutableStateOf<List<MobileWork>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var failed by remember { mutableStateOf(false) }
    var offline by remember { mutableStateOf(false) }
    var showAll by remember { mutableStateOf(false) }
    var lastSync by remember { mutableStateOf("") }
    var selectedWorkId by remember { mutableStateOf<Int?>(null) }
    val snackbarHostState = remember { SnackbarHostState() }
    val worksListState = rememberLazyListState()
    val context = LocalContext.current
    val workDao = remember(context) { SkyLabDatabase.get(context).cachedWorks() }
    val scope = rememberCoroutineScope()

    selectedWorkId?.let { workId ->
        MobileWorkDetailScreen(username = username, token = token, workId = workId, workDao = workDao, onBack = { selectedWorkId = null }, modifier = modifier)
        return
    }

    val synchronize: () -> Unit = {
        scope.launch {
            loading = true
            failed = false
            offline = false
            try {
                val previousIds = works.mapTo(mutableSetOf()) { it.id }
                val synchronizedWorks = synchronizeMyWorks(token, username, workDao)
                val newCount = synchronizedWorks.count { it.id !in previousIds }
                works = synchronizedWorks
                lastSync = LocalDateTime.now().format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm:ss"))
                val message = if (newCount == 0) {
                    "Sincronizzazione completata — elenco invariato"
                } else {
                    "Sincronizzazione completata — $newCount ${if (newCount == 1) "nuovo lavoro" else "nuovi lavori"}"
                }
                snackbarHostState.showSnackbar(message, duration = SnackbarDuration.Short)
            } catch (_: Exception) {
                if (works.isEmpty()) failed = true else offline = true
            } finally {
                loading = false
            }
        }
        Unit
    }

    LaunchedEffect(token) {
        val cachedWorks = loadCachedWorks(username, workDao)
        if (cachedWorks.isNotEmpty()) {
            works = cachedWorks
            val cachedAt = withContext(Dispatchers.IO) { workDao.lastSynchronization(username) }
            if (cachedAt != null) lastSync = formatSyncTimestamp(cachedAt)
        }
        try {
            works = synchronizeMyWorks(token, username, workDao)
            lastSync = LocalDateTime.now().format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm:ss"))
        } catch (_: Exception) {
            if (works.isEmpty()) failed = true else offline = true
        } finally {
            loading = false
        }
    }

    Box(modifier = modifier.fillMaxSize()) {
    Column(modifier = Modifier.fillMaxSize().padding(20.dp)) {
        OutlinedButton(onClick = onBack) { Text("← Indietro", fontSize = 17.sp) }
        Spacer(modifier = Modifier.height(18.dp))
        Text("Lavori assegnati", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
        Spacer(modifier = Modifier.height(12.dp))
        Button(
            onClick = synchronize,
            enabled = !loading,
            modifier = Modifier.fillMaxWidth().height(52.dp)
        ) {
            Text(if (loading) "Sincronizzazione…" else "Sincronizza lavori", fontSize = 18.sp)
        }
        Text(
            text = if (lastSync.isBlank()) "Ultima sincronizzazione: non disponibile" else "Ultima sincronizzazione: $lastSync",
            style = MaterialTheme.typography.bodyMedium,
            modifier = Modifier.padding(top = 6.dp)
        )
        Spacer(modifier = Modifier.height(12.dp))
        val today = LocalDate.now().toString()
        val todayCount = works.count { it.plannedOn.take(10) == today }
        Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            FilterChip(
                selected = !showAll,
                onClick = { showAll = false },
                label = { Text("Oggi ($todayCount)", fontSize = 17.sp) }
            )
            FilterChip(
                selected = showAll,
                onClick = { showAll = true },
                label = { Text("Tutti (${works.size})", fontSize = 17.sp) }
            )
        }
        Spacer(modifier = Modifier.height(16.dp))
        if (failed) Text("Impossibile sincronizzare i lavori", color = MaterialTheme.colorScheme.error, fontSize = 17.sp)
        if (offline) Text("Modalità offline — visualizzazione dell’ultima copia salvata", color = MaterialTheme.colorScheme.primary, fontSize = 16.sp)
        when {
            loading && works.isEmpty() -> CircularProgressIndicator(modifier = Modifier.align(Alignment.CenterHorizontally))
            works.isEmpty() -> Text("Nessun lavoro assegnato", fontSize = 18.sp)
            else -> {
                val orderedWorks = works.sortedWith(
                    compareBy<MobileWork> { it.dateKey }
                        .thenBy { it.plannedAt.ifBlank { "99:99:99" } }
                        .thenBy { it.id }
                )
                val visibleWorks = if (showAll) orderedWorks else orderedWorks.filter { it.plannedOn.take(10) == today }
                if (visibleWorks.isEmpty()) {
                    Text("Nessun lavoro previsto per oggi", fontSize = 18.sp)
                } else LazyColumn(state = worksListState, verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    visibleWorks.groupBy { it.dateKey }.forEach { (date, dayWorks) ->
                        item(key = "date-$date") {
                            Text(
                                text = dayWorks.first().dateLabel,
                                fontSize = 19.sp,
                                fontWeight = FontWeight.Bold,
                                color = MaterialTheme.colorScheme.primary,
                                modifier = Modifier.padding(top = 6.dp)
                            )
                        }
                        items(dayWorks, key = { it.id }) { work ->
                            Card(onClick = { selectedWorkId = work.id }, modifier = Modifier.fillMaxWidth()) {
                                Column(modifier = Modifier.padding(16.dp)) {
                                    Text("Scheda ${work.number}", fontSize = 18.sp, fontWeight = FontWeight.Bold)
                                    Text(work.timeLabel, fontSize = 17.sp)
                                    Spacer(modifier = Modifier.height(6.dp))
                                    Text(work.customer, fontSize = 19.sp, fontWeight = FontWeight.SemiBold)
                                    Text(work.site, fontSize = 16.sp)
                                    if (work.summary.isNotBlank()) Text(work.summary, fontSize = 17.sp, modifier = Modifier.padding(top = 6.dp))
                                }
                            }
                        }
                    }
                }
            }
        }
    }
        SnackbarHost(
            hostState = snackbarHostState,
            modifier = Modifier.align(Alignment.BottomCenter).padding(20.dp)
        )
    }
}

@Composable
private fun MobileWorkDetailScreen(username: String, token: String, workId: Int, workDao: CachedWorkDao, onBack: () -> Unit, modifier: Modifier = Modifier) {
    var detail by remember(workId) { mutableStateOf<MobileWorkDetail?>(null) }
    var failed by remember(workId) { mutableStateOf(false) }
    BackHandler(onBack = onBack)
    LaunchedEffect(workId, token) {
        val cachedJson = workDao.detail(username, workId)
        if (!cachedJson.isNullOrBlank()) detail = parseMobileWorkDetail(cachedJson)
        try {
            val (freshDetail, json) = loadMobileWorkDetail(token, workId)
            detail = freshDetail
            workDao.updateDetail(username, workId, json, System.currentTimeMillis())
        } catch (exception: Exception) {
            failed = detail == null
            android.util.Log.e("SkyLabMobile", "Caricamento dettaglio non riuscito", exception)
        }
    }

    Scaffold(
        modifier = modifier.fillMaxSize(),
        topBar = {
            OutlinedButton(
                onClick = onBack,
                modifier = Modifier.padding(start = 20.dp, top = 20.dp, bottom = 10.dp)
            ) { Text("← Lavori assegnati", fontSize = 17.sp) }
        }
    ) { innerPadding ->
        LazyColumn(
            modifier = Modifier.fillMaxSize().padding(innerPadding).padding(horizontal = 20.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
        when {
            failed -> item { Text("Impossibile caricare il dettaglio del lavoro", color = MaterialTheme.colorScheme.error, fontSize = 17.sp) }
            detail == null -> item { CircularProgressIndicator() }
            else -> {
                val work = detail!!
                item {
                    Text("Scheda ${work.number}", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
                    Text(work.status, color = MaterialTheme.colorScheme.primary, fontSize = 17.sp, fontWeight = FontWeight.SemiBold)
                }
                item {
                    DetailSection("Pianificazione") {
                        DetailValue("Data scheda", formatMobileDate(work.draftedOn))
                        DetailValue("Data intervento", formatMobileDate(work.plannedOn))
                        DetailValue("Ora intervento", formatMobileTime(work.plannedAt))
                        DetailValue("Ultimo intervento", formatMobileDate(work.lastServiceOn))
                        DetailValue("Operatore", work.assignedOperator)
                        if (work.outcome.isNotBlank()) DetailValue("Esito", work.outcome)
                    }
                }
                item {
                    DetailSection("Cliente e sede") {
                        DetailValue("Cliente", work.customer)
                        DetailValue("Sede lavoro", work.site)
                    }
                }
                item {
                    DetailSection("Lavoro") {
                        DetailValue("Sintesi", work.summary.ifBlank { "Non specificata" })
                        DetailValue("Istruzioni operative", work.instructions.ifBlank { "Nessuna istruzione" })
                    }
                }
                item { DetailRowsSection("Prestazioni previste", work.services) }
                item { DetailRowsSection("Materiali previsti", work.materials) }
                item {
                    DetailSection("Preventivo") {
                        DetailValue("Manodopera", formatCurrency(work.plannedLabour))
                        DetailValue("Materiali", formatCurrency(work.plannedMaterials))
                        DetailValue("Totale netto", formatCurrency(work.plannedNet))
                    }
                }
                item { Spacer(modifier = Modifier.height(24.dp)) }
            }
        }
        }
    }
}

@Composable
private fun DetailSection(title: String, content: @Composable () -> Unit) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(7.dp)) {
            Text(title, fontSize = 19.sp, fontWeight = FontWeight.Bold, color = MaterialTheme.colorScheme.primary)
            content()
        }
    }
}

@Composable
private fun DetailValue(label: String, value: String) {
    Row(verticalAlignment = Alignment.Top) {
        Text(
            label,
            style = MaterialTheme.typography.labelMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.width(132.dp).padding(top = 3.dp)
        )
        Text(value.ifBlank { "—" }, fontSize = 17.sp)
    }
}

@Composable
private fun DetailRowsSection(title: String, rows: List<MobileWorkDetailRow>) {
    DetailSection(title) {
        if (rows.isEmpty()) {
            Text("Nessuna voce", fontSize = 17.sp)
        } else rows.forEach { row ->
            Column {
                Text(listOf(row.reference, row.description).filter { it.isNotBlank() }.joinToString(" · "), fontSize = 17.sp, fontWeight = FontWeight.SemiBold)
                Text("Quantità ${formatQuantity(row.quantity)} · ${formatCurrency(row.unitPrice)} · Totale ${formatCurrency(row.amount)}", fontSize = 15.sp)
            }
        }
    }
}

@Preview(showBackground = true)
@Composable
fun LoginScreenPreview() {
    SkyLabMobileTheme {
        LoginScreen(loadUsers = { listOf("tecnico") })
    }
}

private suspend fun loadLoginUsers(): List<String> = withContext(Dispatchers.IO) {
    val connection = URL("http://localhost:5187/api/mobile/login-users")
        .openConnection() as HttpURLConnection
    try {
        connection.connectTimeout = 5_000
        connection.readTimeout = 5_000
        connection.requestMethod = "GET"
        if (connection.responseCode !in 200..299) {
            error("Risposta server ${connection.responseCode}")
        }
        val json = connection.inputStream.bufferedReader().use { it.readText() }
        val array = JSONArray(json)
        List(array.length()) { index -> array.getString(index) }
    } finally {
        connection.disconnect()
    }
}

private suspend fun authenticate(username: String, password: String): String? = withContext(Dispatchers.IO) {
    val connection = URL("http://localhost:5187/api/mobile/login")
        .openConnection() as HttpURLConnection
    try {
        connection.connectTimeout = 5_000
        connection.readTimeout = 5_000
        connection.requestMethod = "POST"
        connection.setRequestProperty("Content-Type", "application/json; charset=utf-8")
        connection.doOutput = true
        val body = JSONObject()
            .put("username", username)
            .put("password", password)
            .toString()
        connection.outputStream.use { it.write(body.toByteArray(Charsets.UTF_8)) }
        if (connection.responseCode !in 200..299) return@withContext null
        val json = connection.inputStream.bufferedReader().use { it.readText() }
        JSONObject(json).getString("token")
    } finally {
        connection.disconnect()
    }
}

private data class MobileWork(
    val id: Int,
    val number: String,
    val plannedOn: String,
    val plannedAt: String,
    val customer: String,
    val site: String,
    val summary: String,
    val status: String
) {
    val dateKey: String get() = plannedOn.take(10).ifBlank { "9999-12-31" }
    val timeLabel: String get() = if (plannedAt.length >= 5) plannedAt.substring(0, 5) else "Ora da definire"
    val dateLabel: String
        get() = try {
            LocalDate.parse(plannedOn.take(10)).format(DateTimeFormatter.ofPattern("EEEE d MMMM yyyy", Locale.ITALIAN))
                .replaceFirstChar { it.uppercase() }
        } catch (_: Exception) {
            "Data da definire"
        }
}

private data class MobileWorkDetailRow(
    val reference: String,
    val description: String,
    val quantity: Double,
    val unitPrice: Double,
    val amount: Double
)

private data class MobileWorkDetail(
    val id: Int,
    val number: String,
    val draftedOn: String,
    val plannedOn: String,
    val plannedAt: String,
    val lastServiceOn: String,
    val customer: String,
    val site: String,
    val assignedOperator: String,
    val status: String,
    val outcome: String,
    val summary: String,
    val instructions: String,
    val plannedLabour: Double,
    val plannedMaterials: Double,
    val plannedNet: Double,
    val services: List<MobileWorkDetailRow>,
    val materials: List<MobileWorkDetailRow>
)

private fun formatMobileDate(value: String): String = try {
    LocalDate.parse(value.take(10)).format(DateTimeFormatter.ofPattern("dd/MM/yyyy"))
} catch (_: Exception) { "—" }

private fun formatMobileTime(value: String): String = if (value.length >= 5) value.substring(0, 5) else "—"
private fun formatCurrency(value: Double): String = NumberFormat.getCurrencyInstance(Locale.ITALY).format(value)
private fun formatQuantity(value: Double): String = NumberFormat.getNumberInstance(Locale.ITALY).apply { maximumFractionDigits = 3 }.format(value)
private fun formatSyncTimestamp(value: Long): String = LocalDateTime.ofInstant(Instant.ofEpochMilli(value), ZoneId.systemDefault())
    .format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm:ss"))

private fun CachedWorkEntity.toMobileWork() = MobileWork(workId, number, plannedOn, plannedAt, customer, site, summary, status)

private fun MobileWork.toCache(username: String, detailJson: String?) = CachedWorkEntity(
    username = username, workId = id, number = number, plannedOn = plannedOn, plannedAt = plannedAt,
    customer = customer, site = site, summary = summary, status = status, detailJson = detailJson,
    synchronizedAt = System.currentTimeMillis()
)

private suspend fun loadCachedWorks(username: String, dao: CachedWorkDao): List<MobileWork> =
    withContext(Dispatchers.IO) { dao.works(username).map { it.toMobileWork() } }

private suspend fun synchronizeMyWorks(token: String, username: String, dao: CachedWorkDao): List<MobileWork> {
    val existingDetails = withContext(Dispatchers.IO) { dao.works(username).associate { it.workId to it.detailJson } }
    val remoteWorks = loadMyWorks(token)
    val cachedItems = remoteWorks.map { work ->
        val detailJson = try {
            loadMobileWorkDetail(token, work.id).second
        } catch (_: Exception) {
            existingDetails[work.id]
        }
        work.toCache(username, detailJson)
    }
    withContext(Dispatchers.IO) {
        dao.replaceForUser(username, cachedItems)
    }
    return remoteWorks
}

private suspend fun loadMyWorks(token: String): List<MobileWork> = withContext(Dispatchers.IO) {
    val connection = URL("http://localhost:5187/api/mobile/my-works").openConnection() as HttpURLConnection
    try {
        connection.connectTimeout = 5_000
        connection.readTimeout = 5_000
        connection.requestMethod = "GET"
        connection.setRequestProperty("Authorization", "Bearer $token")
        if (connection.responseCode !in 200..299) error("Risposta server ${connection.responseCode}")
        val array = JSONArray(connection.inputStream.bufferedReader().use { it.readText() })
        List(array.length()) { index ->
            val item = array.getJSONObject(index)
            MobileWork(
                id = item.getInt("id"),
                number = item.getString("number"),
                plannedOn = item.optString("plannedOn"),
                plannedAt = item.optString("plannedAt"),
                customer = item.getString("customer"),
                site = item.getString("site"),
                summary = item.getString("summary"),
                status = item.getString("status")
            )
        }
    } finally {
        connection.disconnect()
    }
}

private suspend fun loadMobileWorkDetail(token: String, workId: Int): Pair<MobileWorkDetail, String> = withContext(Dispatchers.IO) {
    val connection = URL("http://localhost:5187/api/mobile/my-works/$workId").openConnection() as HttpURLConnection
    try {
        connection.connectTimeout = 5_000
        connection.readTimeout = 5_000
        connection.requestMethod = "GET"
        connection.setRequestProperty("Authorization", "Bearer $token")
        if (connection.responseCode !in 200..299) error("Risposta server ${connection.responseCode}")
        val json = connection.inputStream.bufferedReader().use { it.readText() }
        parseMobileWorkDetail(json) to json
    } finally {
        connection.disconnect()
    }
}

private fun parseMobileWorkDetail(json: String): MobileWorkDetail {
    val item = JSONObject(json)
    fun rows(name: String): List<MobileWorkDetailRow> {
        val array = item.getJSONArray(name)
        return List(array.length()) { index ->
            val row = array.getJSONObject(index)
            MobileWorkDetailRow(
                reference = row.optString("reference"), description = row.optString("description"),
                quantity = row.optDouble("quantity"), unitPrice = row.optDouble("unitPrice"), amount = row.optDouble("amount")
            )
        }
    }
    return MobileWorkDetail(
        id = item.getInt("id"), number = item.getString("number"),
        draftedOn = item.optString("draftedOn"), plannedOn = item.optString("plannedOn"), plannedAt = item.optString("plannedAt"),
        lastServiceOn = item.optString("lastServiceOn"), customer = item.optString("customer"), site = item.optString("site"),
        assignedOperator = item.optString("assignedOperator"), status = item.optString("status"), outcome = item.optString("outcome"),
        summary = item.optString("summary"), instructions = item.optString("instructions"),
        plannedLabour = item.optDouble("plannedLabour"), plannedMaterials = item.optDouble("plannedMaterials"), plannedNet = item.optDouble("plannedNet"),
        services = rows("services"), materials = rows("materials")
    )
}
