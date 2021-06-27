create table if not exists AccountKeys
(
	KeyId varchar(256) not null primary key,
	PrivateKeyBase64Url varchar not null,
	CreatedUtc timestamp with time zone not null
);

create table if not exists KeyAuthorization
(
	Token varchar(256) not null primary key,
	KeyAuth varchar not null,
	Host varchar(256) not null,
	CreatedUtc timestamp with time zone not null
);

create table if not exists CertificateWithKey
(
	Id serial primary key,
	Host varchar(256) not null,
	PrivateKeyBase64Url varchar not null,
	CertificateBase64Url varchar not null,
	CreatedUtc timestamp with time zone not null,
	Expiry timestamp not null
);

create table if not exists WhitelistEntry
(
	Host varchar(256) not null primary key,
	CreatedUtc timestamp not null
);