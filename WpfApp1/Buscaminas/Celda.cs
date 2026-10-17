using WpfApp1.Modelos;

namespace WpfApp1.Buscaminas
{
    public enum EstadoCelda
    {
        Oculta,
        Bandera,
        Revelada
    }

    /// <summary>
    /// Lo que se dibuja en la casilla (la ventana elige el aspecto según este valor).
    /// </summary>
    public enum VistaCelda
    {
        Oculta,
        Bandera,
        Vacia,
        Numero,
        Mina,
        MinaExplotada,
        BanderaIncorrecta
    }

    /// <summary>
    /// Una casilla del tablero.
    /// </summary>
    public sealed class Celda : Observable
    {
        private EstadoCelda estado = EstadoCelda.Oculta;
        private bool explotada;
        private bool mostrarMina;
        private bool banderaIncorrecta;

        public Celda(int fila, int columna)
        {
            Fila = fila;
            Columna = columna;
        }

        public int Fila { get; }

        public int Columna { get; }

        public bool EsMina { get; internal set; }

        public int MinasAlrededor { get; internal set; }

        public EstadoCelda Estado
        {
            get => estado;
            internal set
            {
                if (Asignar(ref estado, value))
                {
                    Notificar(nameof(Vista));
                }
            }
        }

        // La mina que se ha pisado.
        public bool Explotada
        {
            get => explotada;
            internal set
            {
                if (Asignar(ref explotada, value))
                {
                    Notificar(nameof(Vista));
                }
            }
        }

        // Al perder se enseñan todas las minas.
        public bool MostrarMina
        {
            get => mostrarMina;
            internal set
            {
                if (Asignar(ref mostrarMina, value))
                {
                    Notificar(nameof(Vista));
                }
            }
        }

        // Al perder se marcan las banderas que no tenían mina.
        public bool BanderaIncorrecta
        {
            get => banderaIncorrecta;
            internal set
            {
                if (Asignar(ref banderaIncorrecta, value))
                {
                    Notificar(nameof(Vista));
                }
            }
        }

        public VistaCelda Vista
        {
            get
            {
                if (Explotada)
                {
                    return VistaCelda.MinaExplotada;
                }

                if (BanderaIncorrecta)
                {
                    return VistaCelda.BanderaIncorrecta;
                }

                if (Estado == EstadoCelda.Bandera)
                {
                    return VistaCelda.Bandera;
                }

                if (MostrarMina)
                {
                    return VistaCelda.Mina;
                }

                if (Estado == EstadoCelda.Revelada)
                {
                    return MinasAlrededor == 0 ? VistaCelda.Vacia : VistaCelda.Numero;
                }

                return VistaCelda.Oculta;
            }
        }

        // Prepara la casilla para una partida nueva.
        internal void Reiniciar()
        {
            EsMina = false;
            MinasAlrededor = 0;
            Explotada = false;
            MostrarMina = false;
            BanderaIncorrecta = false;
            Estado = EstadoCelda.Oculta;
            Notificar(nameof(MinasAlrededor));
        }

        internal void ActualizarNumero()
        {
            Notificar(nameof(MinasAlrededor));
        }
    }
}
