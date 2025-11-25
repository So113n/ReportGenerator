-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Хост: 127.0.0.1
-- Время создания: Сен 05 2025 г., 18:01
-- Версия сервера: 10.4.32-MariaDB
-- Версия PHP: 8.0.30

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- База данных: `461_db`
--

-- --------------------------------------------------------

--
-- Структура таблицы `report_inscents`
--

CREATE TABLE `report_inscents` (
  `id` int(11) NOT NULL,
  `incident_number` int(11) NOT NULL,
  `registration_time` varchar(10) NOT NULL,
  `service` varchar(250) NOT NULL,
  `short_description` varchar(250) NOT NULL,
  `applicant` varchar(100) NOT NULL,
  `priority` int(11) NOT NULL,
  `executor` varchar(100) NOT NULL,
  `decision_time` varchar(10) NOT NULL,
  `status` varchar(20) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Дамп данных таблицы `report_inscents`
--

INSERT INTO `report_inscents` (`id`, `incident_number`, `registration_time`, `service`, `short_description`, `applicant`, `priority`, `executor`, `decision_time`, `status`) VALUES
(1, 1001, '2025-09-01', 'CRM система', 'Проблема с доступом к модулю клиентов', 'Иванов И.И.', 1, 'Петров П.П.', '2025-09-01', '3'),
(2, 1002, '2025-08-31', 'Почтовый сервер', 'Не приходят входящие письма', 'Сидоров А.В.', 2, 'Козлов М.С.', '2025-08-31', '3'),
(3, 1003, '2025-08-27', 'База данных', 'Медленная работа запросов', 'Попова О.Л.', 1, 'Смирнов Д.А.', '2025-08-27', '3'),
(4, 1004, '2025-08-18', 'Веб-сайт', 'Ошибка 500 на главной странице', 'Кузнецов Р.Н.', 2, 'Орлова Е.П.', '2025-08-18', '3'),
(5, 1005, '2025-08-01', 'Мобильное приложение', 'Крах приложения при запуске', 'Федоров С.М.', 1, 'Никитин А.Б.', '2025-08-01', '3'),
(6, 1006, '2025-07-01', 'Система отчетности', 'Не генерируются PDF отчеты', 'Захарова Т.В.', 2, 'Белов И.К.', '2025-07-01', '3'),
(7, 1007, '2025-06-01', 'Сервер авторизации', 'Ошибка аутентификации пользователей', 'Громов П.С.', 1, 'Соколова М.Д.', '2025-06-01', '3'),
(8, 1008, '2025-05-01', 'Файловое хранилище', 'Не загружаются файлы больше 10МБ', 'Тихонов В.Р.', 2, 'Павлова А.Н.', '2025-05-01', '3'),
(9, 1009, '2025-03-01', 'Система бронирования', 'Дублирование бронирований', 'Морозова Л.К.', 1, 'Виноградов С.П.', '2025-03-01', '3'),
(10, 1010, '2024-09-01', 'Платежная система', 'Ошибка при обработке платежей', 'Семенов Д.Л.', 1, 'Ковалева И.С.', '2024-09-01', '3');

--
-- Индексы сохранённых таблиц
--

--
-- Индексы таблицы `report_inscents`
--
ALTER TABLE `report_inscents`
  ADD PRIMARY KEY (`id`);

--
-- AUTO_INCREMENT для сохранённых таблиц
--

--
-- AUTO_INCREMENT для таблицы `report_inscents`
--
ALTER TABLE `report_inscents`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
