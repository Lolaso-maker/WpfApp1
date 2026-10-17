namespace WpfApp1.Buscaminas
{
    public enum EstadoPartida
    {
        Lista,      // Aún no se ha hecho el primer clic.
        EnCurso,
        Ganada,
        Perdida
    }

    /// <summary>
    /// Lógica completa del Buscaminas. No sabe nada de la interfaz:
    /// la ventana solo llama a Revelar / AlternarBandera y dibuja las celdas.
    /// </summary>
    public sealed class Tablero
    {
        private readonly Celda[,] matriz;
        private readonly List<Celda> celdas = new();
        private readonly Random azar;
        private readonly bool primerClicSeguro;

        public Tablero(Dificultad dificultad, bool primerClicSeguro, Random? azar = null)
            : this(
                Dificultades.Filas(dificultad),
                Dificultades.Columnas(dificultad),
                Dificultades.Minas(dificultad),
                primerClicSeguro,
                azar)
        {
            Dificultad = dificultad;
        }

        public Tablero(int filas, int columnas, int minas, bool primerClicSeguro = true, Random? azar = null)
        {
            if (filas < 2 || columnas < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(filas), "El tablero es demasiado pequeño.");
            }

            if (minas < 1 || minas >= filas * columnas)
            {
                throw new ArgumentOutOfRangeException(nameof(minas), "Número de minas no válido.");
            }

            Filas = filas;
            Columnas = columnas;
            Minas = minas;
            this.primerClicSeguro = primerClicSeguro;
            this.azar = azar ?? new Random();

            matriz = new Celda[filas, columnas];

            for (int f = 0; f < filas; f++)
            {
                for (int c = 0; c < columnas; c++)
                {
                    var celda = new Celda(f, c);
                    matriz[f, c] = celda;
                    celdas.Add(celda);
                }
            }
        }

        // Se lanza después de cualquier cambio (para refrescar contador y estado).
        public event EventHandler? Cambiado;

        // Se lanza una sola vez, al ganar o perder.
        public event EventHandler? PartidaTerminada;

        public Dificultad Dificultad { get; }

        public int Filas { get; }

        public int Columnas { get; }

        public int Minas { get; }

        public IReadOnlyList<Celda> Celdas => celdas;

        public EstadoPartida Estado { get; private set; } = EstadoPartida.Lista;

        public int Banderas { get; private set; }

        public int MinasRestantes => Minas - Banderas;

        public int CeldasReveladas { get; private set; }

        public bool SeHanUsadoBanderas { get; private set; }

        public bool Terminada => Estado is EstadoPartida.Ganada or EstadoPartida.Perdida;

        public Celda this[int fila, int columna] => matriz[fila, columna];

        // ───────────────────────── Acciones del jugador ─────────────────────────

        /// <summary>
        /// Clic izquierdo. En una casilla oculta la descubre; en un número ya
        /// descubierto con todas sus banderas puestas, descubre las vecinas.
        /// Devuelve true si ha cambiado algo.
        /// </summary>
        public bool Revelar(Celda celda)
        {
            if (Terminada || celda.Estado == EstadoCelda.Bandera)
            {
                return false;
            }

            if (Estado == EstadoPartida.Lista)
            {
                ColocarMinas(celda);
                Estado = EstadoPartida.EnCurso;
            }

            if (celda.Estado == EstadoCelda.Revelada)
            {
                return Despejar(celda);
            }

            if (celda.EsMina)
            {
                Perder(celda);
                return true;
            }

            Destapar(celda);
            ComprobarVictoria();
            Cambiado?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Clic derecho: pone o quita una bandera.
        /// </summary>
        public bool AlternarBandera(Celda celda)
        {
            if (Terminada || celda.Estado == EstadoCelda.Revelada)
            {
                return false;
            }

            if (celda.Estado == EstadoCelda.Oculta)
            {
                celda.Estado = EstadoCelda.Bandera;
                Banderas++;
                SeHanUsadoBanderas = true;
            }
            else
            {
                celda.Estado = EstadoCelda.Oculta;
                Banderas--;
            }

            Cambiado?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Deja el tablero listo para otra partida con el mismo tamaño.
        /// </summary>
        public void Reiniciar()
        {
            foreach (var celda in celdas)
            {
                celda.Reiniciar();
            }

            Estado = EstadoPartida.Lista;
            Banderas = 0;
            CeldasReveladas = 0;
            SeHanUsadoBanderas = false;
            Cambiado?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<Celda> Vecinas(Celda celda)
        {
            for (int df = -1; df <= 1; df++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (df == 0 && dc == 0)
                    {
                        continue;
                    }

                    int f = celda.Fila + df;
                    int c = celda.Columna + dc;

                    if (f >= 0 && f < Filas && c >= 0 && c < Columnas)
                    {
                        yield return matriz[f, c];
                    }
                }
            }
        }

        // ───────────────────────── Funcionamiento interno ─────────────────────────

        // Las minas se colocan con el primer clic para que nunca se pierda
        // en la primera jugada (si está activado "primer clic seguro").
        private void ColocarMinas(Celda primera)
        {
            var excluidas = new HashSet<Celda>();

            if (primerClicSeguro)
            {
                excluidas.Add(primera);

                foreach (var vecina in Vecinas(primera))
                {
                    excluidas.Add(vecina);
                }

                // En tableros muy llenos solo se protege la casilla pulsada.
                if (celdas.Count - excluidas.Count < Minas)
                {
                    excluidas.Clear();
                    excluidas.Add(primera);
                }
            }

            var candidatas = celdas.Where(c => !excluidas.Contains(c)).ToList();

            // Barajado de Fisher-Yates: las primeras "Minas" posiciones son las minas.
            for (int i = 0; i < Minas; i++)
            {
                int j = azar.Next(i, candidatas.Count);
                (candidatas[i], candidatas[j]) = (candidatas[j], candidatas[i]);
                candidatas[i].EsMina = true;
            }

            CalcularNumeros();
        }

        // Solo para pruebas: coloca las minas en posiciones concretas.
        internal void PrepararConMinas(IEnumerable<(int Fila, int Columna)> posiciones)
        {
            Reiniciar();

            int total = 0;

            foreach (var (fila, columna) in posiciones)
            {
                matriz[fila, columna].EsMina = true;
                total++;
            }

            if (total != Minas)
            {
                throw new InvalidOperationException("El número de minas no coincide.");
            }

            CalcularNumeros();
            Estado = EstadoPartida.EnCurso;
        }

        private void CalcularNumeros()
        {
            foreach (var celda in celdas)
            {
                celda.MinasAlrededor = Vecinas(celda).Count(v => v.EsMina);
                celda.ActualizarNumero();
            }
        }

        // Descubre una casilla y, si es un 0, todas las conectadas (sin recursión).
        private void Destapar(Celda inicio)
        {
            var pendientes = new Stack<Celda>();
            pendientes.Push(inicio);

            while (pendientes.Count > 0)
            {
                var celda = pendientes.Pop();

                if (celda.Estado != EstadoCelda.Oculta || celda.EsMina)
                {
                    continue;
                }

                celda.Estado = EstadoCelda.Revelada;
                CeldasReveladas++;

                if (celda.MinasAlrededor == 0)
                {
                    foreach (var vecina in Vecinas(celda))
                    {
                        if (vecina.Estado == EstadoCelda.Oculta)
                        {
                            pendientes.Push(vecina);
                        }
                    }
                }
            }
        }

        // "Acorde": clic sobre un número con tantas banderas alrededor como su valor.
        private bool Despejar(Celda numero)
        {
            if (numero.MinasAlrededor == 0)
            {
                return false;
            }

            var vecinas = Vecinas(numero).ToList();
            int banderas = vecinas.Count(v => v.Estado == EstadoCelda.Bandera);

            if (banderas != numero.MinasAlrededor)
            {
                return false;
            }

            var ocultas = vecinas.Where(v => v.Estado == EstadoCelda.Oculta).ToList();

            if (ocultas.Count == 0)
            {
                return false;
            }

            var mina = ocultas.FirstOrDefault(v => v.EsMina);

            if (mina is not null)
            {
                Perder(mina);
                return true;
            }

            foreach (var celda in ocultas)
            {
                Destapar(celda);
            }

            ComprobarVictoria();
            Cambiado?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private void Perder(Celda pisada)
        {
            Estado = EstadoPartida.Perdida;
            pisada.Explotada = true;

            foreach (var celda in celdas)
            {
                if (celda.EsMina && celda.Estado != EstadoCelda.Bandera)
                {
                    celda.MostrarMina = true;
                }
                else if (!celda.EsMina && celda.Estado == EstadoCelda.Bandera)
                {
                    celda.BanderaIncorrecta = true;
                }
            }

            Cambiado?.Invoke(this, EventArgs.Empty);
            PartidaTerminada?.Invoke(this, EventArgs.Empty);
        }

        private void ComprobarVictoria()
        {
            if (Estado != EstadoPartida.EnCurso || CeldasReveladas != celdas.Count - Minas)
            {
                return;
            }

            Estado = EstadoPartida.Ganada;

            // Al ganar se marcan automáticamente todas las minas.
            foreach (var celda in celdas)
            {
                if (celda.EsMina && celda.Estado != EstadoCelda.Bandera)
                {
                    celda.Estado = EstadoCelda.Bandera;
                }
            }

            Banderas = Minas;
            PartidaTerminada?.Invoke(this, EventArgs.Empty);
        }
    }
}
