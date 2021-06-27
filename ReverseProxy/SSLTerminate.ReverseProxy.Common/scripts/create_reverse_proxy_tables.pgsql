create table if not exists RegisteredRoute
(
	Host varchar(256) not null primary key,
	Upstream varchar(2048) not null,
	CreatedUtc timestamp not null
);
