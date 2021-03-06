/*
 * Session.cpp
 *
 *  Created on: 2017. 8. 20.
 *      Author: kukuta
 */

#include "Session.h"
#include "Link.h"
#include "LinkManager.h"
#include "../Library/MD5.h"
#include "../Library/Random.h"
#include <list>

namespace Gamnet { namespace Network {

std::string Session::GenerateSessionToken(uint32_t session_key)
{
	return md5(Format(session_key, time(NULL), Random::Range(1, 99999999)));
}

Session::Session() :
		session_key(0),
		session_token(""),
		remote_address(NULL),
		expire_time(0),
		link(NULL),
		manager(NULL)
{
}

Session::~Session()
{
}

void Session::AsyncSend(const std::shared_ptr<Buffer>& buffer)
{
	std::shared_ptr<Link> _link = link;
	if(NULL == _link)
	{
		return;
	}
	_link->AsyncSend(buffer);
}
void Session::AsyncSend(const char* data, size_t length)
{
	std::shared_ptr<Link> _link = link;
	if (NULL == _link)
	{
		return;
	}
	_link->AsyncSend(data, length);
}
int Session::SyncSend(const std::shared_ptr<Buffer>& buffer)
{
	std::shared_ptr<Link> _link = link;
	if (NULL == _link)
	{
		return -1;
	}
	return _link->SyncSend(buffer);
}
int Session::SyncSend(const char* data, size_t length)
{
	std::shared_ptr<Link> _link = link;
	if (NULL == _link)
	{
		return -1;
	}
	return _link->SyncSend(data, length);
}

std::atomic<uint32_t> SessionManager::session_key;

SessionManager::SessionManager() : _keepalive_time(0)
{
}

SessionManager::~SessionManager()
{
}

bool SessionManager::Init(int keepAliveSeconds)
{
	if(0 == keepAliveSeconds)
	{
		return true;
	}
	_keepalive_time = keepAliveSeconds;
	if (false == _timer.SetTimer(5000, [this](){
		std::list<std::shared_ptr<Session>> sessions;
		time_t now_ = time(NULL);
		std::lock_guard<std::recursive_mutex> lo(_lock);
		for(auto itr = _sessions.begin(); itr != _sessions.end();) {
			std::shared_ptr<Session> session = itr->second;
			if(NULL == session->link && session->expire_time + _keepalive_time < now_)
			{
				LOG(GAMNET_ERR, "[session_key:", session->session_key, "] destroy idle session");
				sessions.push_back(session);
			        _sessions.erase(itr++);
			}
	 		else { 
				++itr; 
			}
		}
		_timer.Resume();
		for(auto session : sessions)
		{
			session->OnDestroy();	
		}
	}))
	{
		LOG(GAMNET_ERR, "session time out timer init fail");
		return false;
	}
	return true;
}

bool SessionManager::Add(uint32_t key, const std::shared_ptr<Session>& session)
{
	std::lock_guard<std::recursive_mutex> lo(_lock);
	if(false == _sessions.insert(std::make_pair(key, session)).second)
	{
		LOG(GAMNET_ERR, "[link_key:", session->link->link_key, ", session_key:", key, "] duplicated session key");
		return false;
	}
	session->expire_time = time(NULL);
	return true;
}

void SessionManager::Remove(uint32_t key)
{
	std::shared_ptr<Session> session;
	{
		std::lock_guard<std::recursive_mutex> lo(_lock);
		auto itr = _sessions.find(key);
		if(_sessions.end() == itr)
		{
			return;
		}
		session = itr->second;
		_sessions.erase(key);
	}
	
	session->OnDestroy();
}

std::shared_ptr<Session> SessionManager::Find(uint32_t key)
{
	std::lock_guard<std::recursive_mutex> lo(_lock);
	auto itr = _sessions.find(key);
	if(_sessions.end() == itr)
	{
		return NULL;
	}
	return itr->second;
}

size_t SessionManager::Size()
{
	std::lock_guard<std::recursive_mutex> lo(_lock);
	return _sessions.size();
}
}} /* namespace Gamnet */
